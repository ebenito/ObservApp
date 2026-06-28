using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Models;

namespace ObservApp.Services;

// NOTA DE UBICACIÓN: este archivo vive físicamente en
// ObservApp/Platforms/Windows/ para que el SDK de MAUI lo compile
// EXCLUSIVAMENTE en el TargetFramework net10.0-windows*, sin necesidad de
// guardas #if. El namespace se mantiene como ObservApp.Services para que
// el registro en MauiProgram.cs sea simétrico con el resto de servicios de
// plataforma del proyecto (MauiSettingsService, MauiGeolocationService...).

/// <summary>
/// Implementación de <see cref="ICameraService"/> para Windows usando
/// <c>libgphoto2</c> a través del binario CLI <c>gphoto2.exe</c> (build
/// portable para Windows del proyecto libgphoto2/gphoto2). Conecta vía USB
/// y es agnóstica de fabricante: soporta cualquier cámara compatible con
/// PTP/MTP estándar reconocida por gphoto2 (Canon, Nikon, Sony, Fujifilm,
/// Panasonic, Olympus...) sin usar ningún SDK propietario.
///
/// DECISIÓN DE DISEÑO — por qué CLI y no P/Invoke directo:
///  1. No existe un build oficial con ABI estable de libgphoto2 para Windows
///     entre versiones; el binario CLI absorbe esos cambios y se actualiza
///     de forma independiente a ObservApp.
///  2. El CLI ya resuelve la detección de drivers libusb/WinUSB para la
///     cámara conectada, evitando reimplementar esa capa en C#.
///
/// Se incluyen como referencia, sin uso activo, los stubs P/Invoke de
/// <see cref="NativeGPhoto2"/> por si en el futuro se bundlea un build
/// nativo de libgphoto2.dll y se quiere evitar el coste de lanzar un
/// proceso por cada comando.
/// </summary>
public sealed class GPhoto2CameraService : ICameraService, IDisposable
{
    /// <summary>
    /// Ruta al ejecutable gphoto2.exe. Por defecto se busca primero un build
    /// empaquetado junto al ejecutable de ObservApp en "Tools\gphoto2\gphoto2.exe"
    /// (recomendado para distribución MSIX, sin depender de instalación previa
    /// del usuario); si no existe, se confía en que esté disponible en el PATH.
    /// </summary>
    public string ExecutablePath { get; set; } = ResolveDefaultExecutablePath();

    // Serializa las llamadas al CLI: gphoto2 no soporta bien comandos
    // concurrentes sobre la misma cámara USB (un único canal PTP por sesión).
    private readonly SemaphoreSlim _cliLock = new(1, 1);

    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public string? LastError { get; private set; }
    public event Action<bool>? ConnectionChanged;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            // --auto-detect lista las cámaras USB reconocidas por libgphoto2.
            // Salida típica cuando hay una cámara:
            //   Model                          Port
            //   ----------------------------------------------------------
            //   Canon EOS 90D                  usb:001,004
            var (exitCode, stdOut, stdErr) = await RunCliAsync("--auto-detect", cancellationToken);

            var lineCount = stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            var hasCamera = exitCode == 0 && lineCount > 2; // cabecera + separador + ≥1 cámara

            SetConnected(hasCamera);

            if (!hasCamera)
                LastError = string.IsNullOrWhiteSpace(stdErr)
                    ? "No se detectó ninguna cámara por USB (gphoto2 --auto-detect vacío)."
                    : stdErr;

            return hasCamera;
        }
        catch (Exception ex)
        {
            LastError = $"Error al detectar cámara: {ex.Message}";
            SetConnected(false);
            return false;
        }
    }

    public async Task SetExposureAsync(ExposureSetting settings)
    {
        LastError = null;

        // Timeout defensivo: si el proceso gphoto2 se cuelga (p. ej. la
        // cámara entró en modo de ahorro de energía a media noche), no debe
        // bloquear indefinidamente el temporizador de eventos del eclipse.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Las claves de configuración (aperture/iso/shutterspeed) son las
        // habituales que libgphoto2 expone vía PTP para la mayoría de
        // réflex/mirrorless. --set-config-value evita la confirmación
        // interactiva que pide --set-config en listas de valores.
        var aperture = NormalizeApertureValue(settings.Aperture);
        var args = $"--set-config-value aperture={aperture} " +
                   $"--set-config-value iso={settings.Iso} " +
                   $"--set-config-value shutterspeed={settings.Shutter}";

        var (exitCode, _, stdErr) = await RunCliAsync(args, timeoutCts.Token);

        if (exitCode != 0)
            LastError = $"No se pudieron aplicar los ajustes de exposición: {stdErr}";
    }

    public async Task TriggerCaptureAsync(CancellationToken ct)
    {
        LastError = null;

        // --trigger-capture dispara SIN descargar el archivo a disco (más
        // rápido que --capture-image-and-download), ideal para series
        // rápidas como el bracket de 8 tomas de corona. La imagen queda en
        // la tarjeta de la cámara, que es justo lo que se quiere durante un
        // eclipse: cero tiempo perdido en transferencias USB.
        var (exitCode, _, stdErr) = await RunCliAsync("--trigger-capture", ct);

        if (exitCode != 0)
            LastError = $"Fallo al disparar: {stdErr}";
    }

    public Task DisconnectAsync()
    {
        SetConnected(false);
        return Task.CompletedTask;
    }

    // ── Ejecución del proceso CLI ────────────────────────────────────────────

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(
        string arguments, CancellationToken cancellationToken)
    {
        await _cliLock.WaitAsync(cancellationToken);
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ExecutablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
            process.ErrorDataReceived  += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode, stdOut.ToString(), stdErr.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // No relanzamos: SetExposureAsync/TriggerCaptureAsync exponen el
            // fallo vía LastError en lugar de tirar la secuencia del eclipse
            // por una excepción no controlada en medio de una ráfaga.
            return (-1, string.Empty, ex.Message);
        }
        finally
        {
            _cliLock.Release();
        }
    }

    private void SetConnected(bool connected)
    {
        if (_isConnected == connected) return;
        _isConnected = connected;
        ConnectionChanged?.Invoke(connected);
    }

    /// <summary>"f/8" → "8"; gphoto2 espera solo el número del f-stop.</summary>
    private static string NormalizeApertureValue(string aperture)
    {
        var trimmed = aperture.Trim();
        var idx = trimmed.IndexOf('/');
        return idx >= 0 ? trimmed[(idx + 1)..].Trim() : trimmed;
    }

    private static string ResolveDefaultExecutablePath()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "Tools", "gphoto2", "gphoto2.exe");
        return File.Exists(bundled) ? bundled : "gphoto2.exe"; // confía en el PATH del sistema
    }

    public void Dispose() => _cliLock.Dispose();

    // ── Stubs P/Invoke (no usados por defecto) ──────────────────────────────
    // Punto de extensión genérico para invocar libgphoto2 de forma nativa si
    // en el futuro se bundlea un build .dll para Windows. Firmas alineadas
    // con gphoto2/gphoto2-camera.h y gphoto2/gphoto2-context.h de la librería
    // oficial. No se invocan desde el flujo actual (que usa el CLI).
    private static class NativeGPhoto2
    {
        private const string LibName = "libgphoto2"; // resolvería a libgphoto2.dll si se bundlea

        /// <summary>gp_camera_new(Camera **camera)</summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int gp_camera_new(out IntPtr camera);

        /// <summary>gp_camera_init(Camera *camera, GPContext *context)</summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int gp_camera_init(IntPtr camera, IntPtr context);

        /// <summary>gp_camera_set_config(Camera *camera, CameraWidget *window, GPContext *context)</summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int gp_camera_set_config(IntPtr camera, IntPtr window, IntPtr context);

        /// <summary>gp_camera_capture(Camera *camera, CameraCaptureType type, CameraFilePath *path, GPContext *context)</summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int gp_camera_capture(IntPtr camera, int captureType, IntPtr path, IntPtr context);

        /// <summary>gp_camera_exit(Camera *camera, GPContext *context)</summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int gp_camera_exit(IntPtr camera, IntPtr context);
    }
}
