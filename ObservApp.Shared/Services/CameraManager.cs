using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Orquesta la secuencia de disparos para los eventos del eclipse, delegando
/// el control físico de la cámara en <see cref="ICameraService"/>. No conoce
/// el protocolo de comunicación (USB/libgphoto2, WiFi/PTP-IP) ni de dónde
/// vienen los valores de exposición — solo decide CUÁNDO disparar y CUÁNTAS
/// veces, a partir de la lista de <see cref="ExposureSetting"/> que le pasa
/// el llamador (ver <see cref="EclipseExposureCalculator"/> para cómo se
/// obtienen esos valores a partir de la tabla NASA-Q).
///
/// CAMBIO DE DISEÑO respecto a la versión anterior: el parámetro único
/// <c>ExposureSetting manualSettings</c> se sustituye por
/// <c>IReadOnlyList&lt;ExposureSetting&gt; exposures</c> porque el bracket de
/// totalidad ya no se sintetiza matemáticamente (pasos EV) — ahora son las
/// 8 filas REALES de la tabla Q (Cromosfera → Tierra iluminada). Para
/// eventos normales, <c>exposures</c> simplemente contiene un único elemento.
///
/// Se inyecta mediante DI como Singleton (una única cámara física por
/// sesión de la app) y es thread-safe: el intercambio del
/// <see cref="CancellationTokenSource"/> está protegido por <c>lock</c>.
/// </summary>
public sealed class CameraManager : IDisposable
{
	private readonly ICameraService _camera;
	private readonly object _gate = new();
	private CancellationTokenSource? _currentCts;

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
	/// pendientes del evento previo, sin que el llamador tenga que gestionar
	/// manualmente ningún <see cref="CancellationTokenSource"/>.
	/// </summary>
	/// <param name="photosPerEvent">
	/// Número de tomas a realizar para eventos normales, o número de VECES
	/// que se repite el recorrido completo de <paramref name="exposures"/>
	/// cuando <paramref name="isTotality"/> es <c>true</c>.
	/// </param>
	/// <param name="isTotality">
	/// <c>true</c> durante el intervalo de totalidad/anularidad: en vez de
	/// repetir la misma toma, recorre secuencialmente TODA la lista de
	/// <paramref name="exposures"/> (el bracket de corona) y repite ese
	/// recorrido completo <paramref name="photosPerEvent"/> veces.
	/// </param>
	/// <param name="exposures">
	/// Ajustes a aplicar. En eventos normales, un único elemento (la fila de
	/// la tabla Q resuelta para ese evento). En totalidad, el bracket
	/// completo — típicamente <see cref="EclipseExposureCalculator.GetTotalityBracket"/>.
	/// </param>
	public async Task RunEclipseSequenceAsync(
		int photosPerEvent,
		bool isTotality,
		IReadOnlyList<ExposureSetting> exposures)
	{
		if (exposures.Count == 0) return;

		// Cancela cualquier secuencia anterior — incluida una serie de
		// corona a medio terminar, que queda deliberadamente incompleta y
		// se descarta.
		var cts = SwapCancellationSource();
		var token = cts.Token;

		Progress = 0;
		bool completed = false;

		try
		{
			if (!_camera.IsConnected)
				await _camera.ConnectAsync(token);

			if (isTotality)
				await RunBracketLoopAsync(photosPerEvent, exposures, token);
			else
				await RunSimpleLoopAsync(photosPerEvent, exposures[0], token);

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
	/// Repite <paramref name="repeticiones"/> veces el recorrido completo del
	/// bracket. Cada repetición es atómica: si la cancelación llega a mitad
	/// de una, esa repetición concreta queda incompleta y se abandona sin
	/// intentar terminarla ni reintentarla — exactamente el comportamiento
	/// "esas series se eliminarían" solicitado en la especificación original.
	/// </summary>
	private async Task RunBracketLoopAsync(
		int repeticiones,
		IReadOnlyList<ExposureSetting> bracket,
		CancellationToken token)
	{
		int totalShots = repeticiones * bracket.Count;
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
	/// Cancela de forma segura (thread-safe) cualquier secuencia en curso sin
	/// lanzar una nueva. Pensado para un botón "Detener" explícito en la UI.
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