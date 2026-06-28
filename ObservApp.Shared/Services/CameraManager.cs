using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Orquesta la secuencia de disparos para los eventos del eclipse, delegando
/// el control físico de la cámara en <see cref="ICameraService"/>. No conoce
/// el protocolo de comunicación (USB/libgphoto2, WiFi/PTP-IP) — solo decide
/// CUÁNDO disparar, CON QUÉ AJUSTES, y CUÁNTAS VECES, a partir de los valores
/// ya calculados por la Calculadora de Exposición de Eclipses.
///
/// Se inyecta mediante DI como Singleton (una única cámara física por sesión
/// de la app) y es thread-safe: el intercambio del <see cref="CancellationTokenSource"/>
/// está protegido por <c>lock</c>, de forma que puede invocarse tanto desde la
/// UI (botón "Iniciar") como desde el temporizador que dispara los eventos
/// calculados del eclipse sin condiciones de carrera.
/// </summary>
public sealed class CameraManager : IDisposable
{
    private readonly ICameraService _camera;
    private readonly object _gate = new();
    private CancellationTokenSource? _currentCts;

    /// <summary>
    /// Número de exposiciones del bracket de corona durante la totalidad.
    /// Sigue la práctica estándar de fotografía de eclipses: un abanico de
    /// velocidades de obturación que cubre desde la cromosfera/protuberancias
    /// (tiempos muy cortos) hasta la corona externa difusa (tiempos largos),
    /// manteniendo fijas la apertura y el ISO entre tomas del mismo bracket
    /// (cambiar el diafragma alteraría el enfoque; cambiar el ISO introduciría
    /// ruido variable entre exposiciones que se quieren comparar/combinar en
    /// postproducción tipo HDR).
    /// </summary>
    public const int CoronaBracketSize = 8;

    // Offsets en pasos EV (stops) respecto a la velocidad base indicada por
    // el usuario para la totalidad. Asimétrico hacia tiempos largos: la
    // corona externa es mucho más débil que el disco lunar/cromosfera, así
    // que se necesitan más pasos hacia exposiciones largas que hacia cortas.
    private static readonly int[] CoronaBracketStopsEv = { -4, -3, -2, -1, 0, 1, 2, 4 };

    /// <summary>Progreso 0..1 de la secuencia en curso, para enlazar con la UI.</summary>
    public double Progress { get; private set; }

    /// <summary>Se invoca tras cada disparo individual con el índice (1-based) y el total de la serie actual.</summary>
    public event Action<int, int>? ShotTaken;

    /// <summary>
    /// Se invoca cuando la secuencia termina: <c>true</c> si completó todas
    /// las tomas, <c>false</c> si fue cancelada (típicamente porque llegó un
    /// nuevo evento del eclipse antes de terminar).
    /// </summary>
    public event Action<bool>? SequenceFinished;

    public CameraManager(ICameraService camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Lanza la secuencia de disparos para el evento actual del eclipse.
    /// Cancela automáticamente cualquier secuencia anterior todavía en curso
    /// — un nuevo evento del eclipse siempre tiene prioridad sobre series
    /// pendientes del evento previo, sin necesidad de que el llamador gestione
    /// manualmente ningún <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <param name="photosPerEvent">
    /// Número de tomas a realizar para eventos normales (C1, C4, bandas de
    /// sombra, collar de Baily...), o número de VECES que se repite el bloque
    /// atómico de <see cref="CoronaBracketSize"/> tomas de corona cuando
    /// <paramref name="isTotality"/> es <c>true</c>.
    /// </param>
    /// <param name="isTotality">
    /// <c>true</c> durante el intervalo C2→C3 (totalidad) o el equivalente en
    /// un eclipse anular: en vez de repetir la misma toma, dispara el bracket
    /// de <see cref="CoronaBracketSize"/> exposiciones de corona.
    /// </param>
    /// <param name="manualSettings">
    /// Ajustes calculados por la Calculadora de Exposición de Eclipses
    /// (apertura, ISO, velocidad) para el evento que se está fotografiando.
    /// En totalidad se usa como velocidad base del bracket (ver
    /// <see cref="BuildCoronaBracket"/>); en el resto de eventos se aplica
    /// directamente, sin modificar.
    /// </param>
    public async Task RunEclipseSequenceAsync(
        int photosPerEvent,
        bool isTotality,
        ExposureSetting manualSettings)
    {
        // Cancela cualquier secuencia anterior — incluida una serie de corona
        // a medio terminar, que queda deliberadamente incompleta y se descarta.
        var cts = SwapCancellationSource();
        var token = cts.Token;

        Progress = 0;
        bool completed = false;

        try
        {
            if (!_camera.IsConnected)
                await _camera.ConnectAsync(token);

            if (isTotality)
                await RunTotalityBracketLoopAsync(photosPerEvent, manualSettings, token);
            else
                await RunSimpleLoopAsync(photosPerEvent, manualSettings, token);

            completed = true;
        }
        catch (OperationCanceledException)
        {
            // Esperado: la secuencia fue interrumpida por la llegada de un
            // nuevo evento (o por una llamada explícita a CancelCurrentSequence).
        }
        finally
        {
            SequenceFinished?.Invoke(completed);
        }
    }

    /// <summary>
    /// Serie simple: aplica una única configuración y dispara
    /// <paramref name="photosPerEvent"/> tomas idénticas seguidas.
    /// </summary>
    private async Task RunSimpleLoopAsync(
        int photosPerEvent,
        ExposureSetting settings,
        CancellationToken token)
    {
        await _camera.SetExposureAsync(settings);

        for (int i = 0; i < photosPerEvent; i++)
        {
            token.ThrowIfCancellationRequested();
            await _camera.TriggerCaptureAsync(token);
            ShotTaken?.Invoke(i + 1, photosPerEvent);
            Progress = (i + 1) / (double)Math.Max(1, photosPerEvent);
        }
    }

    /// <summary>
    /// Repite <paramref name="repeticiones"/> veces el bloque atómico de
    /// <see cref="CoronaBracketSize"/> exposiciones de corona. Cada
    /// repetición se trata como un bloque atómico: si la cancelación llega a
    /// mitad de un bloque (porque el cálculo de la app ya pasó al siguiente
    /// evento, p. ej. C3), esa repetición concreta queda incompleta y se
    /// abandona sin intentar terminarla ni reintentarla — exactamente el
    /// comportamiento "esas series se eliminarían" solicitado.
    /// </summary>
    private async Task RunTotalityBracketLoopAsync(
        int repeticiones,
        ExposureSetting baseSettings,
        CancellationToken token)
    {
        var bracket = BuildCoronaBracket(baseSettings);
        int totalShots = repeticiones * CoronaBracketSize;
        int shotsDone = 0;

        for (int rep = 0; rep < repeticiones; rep++)
        {
            token.ThrowIfCancellationRequested();

            foreach (var exposure in bracket)
            {
                token.ThrowIfCancellationRequested();

                await _camera.SetExposureAsync(exposure);
                await _camera.TriggerCaptureAsync(token);

                shotsDone++;
                ShotTaken?.Invoke(shotsDone, totalShots);
                Progress = shotsDone / (double)Math.Max(1, totalShots);
            }
        }
    }

    /// <summary>
    /// Genera el abanico de <see cref="CoronaBracketSize"/> exposiciones de
    /// corona a partir de la velocidad de obturación calculada para la
    /// totalidad, manteniendo fijas apertura e ISO. Público y estático para
    /// que la UI pueda mostrar una vista previa de la tabla de brackets antes
    /// de iniciar el temporizador.
    /// </summary>
    public static List<ExposureSetting> BuildCoronaBracket(ExposureSetting baseSettings)
    {
        var baseSeconds = baseSettings.ShutterSeconds > 0 ? baseSettings.ShutterSeconds : 1.0 / 500;
        var result = new List<ExposureSetting>(CoronaBracketSize);

        foreach (var ev in CoronaBracketStopsEv)
        {
            var seconds = baseSeconds * Math.Pow(2, ev);
            result.Add(new ExposureSetting(
                Shutter: FormatShutter(seconds),
                Iso: baseSettings.Iso,
                Aperture: baseSettings.Aperture));
        }

        return result;
    }

    private static string FormatShutter(double seconds)
    {
        if (seconds <= 0) return "1/500";

        if (seconds >= 1.0)
            return seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "\"";

        var denom = (int)Math.Round(1.0 / seconds);
        return $"1/{Math.Max(1, denom)}";
    }

    /// <summary>
    /// Cancela de forma segura (thread-safe) cualquier secuencia en curso sin
    /// lanzar una nueva. Pensado para un botón "Detener" explícito en la UI,
    /// además de la cancelación automática que ocurre al llamar de nuevo a
    /// <see cref="RunEclipseSequenceAsync"/>.
    /// </summary>
    public void CancelCurrentSequence()
    {
        lock (_gate)
        {
            _currentCts?.Cancel();
        }
    }

    private CancellationTokenSource SwapCancellationSource()
    {
        lock (_gate)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = new CancellationTokenSource();
            return _currentCts;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }
}
