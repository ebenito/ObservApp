using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Implementación de pruebas de <see cref="ICameraService"/>. No se conecta a
/// hardware real — registra cada operación por consola/Debug y simula
/// latencias realistas de ajuste y disparo. Útil para:
///  · Desarrollar y probar <see cref="CameraManager"/> sin una cámara física.
///  · Servir de implementación por defecto en la versión Web, donde
///    gphoto2 (USB) y PTP/IP (WiFi) no se implementan ("En web no hace falta
///    implementarlo").
///  · Hacer demos o QA del temporizador de eventos del eclipse sin arriesgar
///    disparos reales sobre el equipo.
/// </summary>
public sealed class MockCameraService : ICameraService
{
    private readonly object _gate = new();
    private bool _isConnected;
    private int _shotCounter;

    public bool IsConnected
    {
        get { lock (_gate) return _isConnected; }
    }

    public string? LastError { get; private set; }

    public event Action<bool>? ConnectionChanged;

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate) { _isConnected = true; }

        Log("Conectada (simulada).");
        ConnectionChanged?.Invoke(true);
        return Task.FromResult(true);
    }

    public async Task SetExposureAsync(ExposureSetting settings)
    {
        // Latencia simulada de escritura de parámetros en la cámara.
        await Task.Delay(80);
        Log($"Ajustes aplicados → {settings.Aperture} · ISO {settings.Iso} · {settings.Shutter}");
    }

    public async Task TriggerCaptureAsync(CancellationToken ct)
    {
        // Simula el tiempo de disparo + escritura en buffer/tarjeta.
        await Task.Delay(250, ct);
        var n = Interlocked.Increment(ref _shotCounter);
        Log($"📸 Disparo #{n} realizado.");
    }

    public Task DisconnectAsync()
    {
        lock (_gate) { _isConnected = false; }

        Log("Desconectada.");
        ConnectionChanged?.Invoke(false);
        return Task.CompletedTask;
    }

    private static void Log(string message)
    {
        var line = $"[MockCamera] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}
