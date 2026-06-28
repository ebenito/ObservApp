using ObservApp.Shared.Models;

namespace ObservApp.Shared.Interfaces;

/// <summary>
/// Contrato universal para el control remoto de cámaras réflex/mirrorless
/// desde ObservApp. Es la ÚNICA superficie que <c>ObservApp.Shared</c> conoce:
/// ningún componente Razor ni servicio compartido debe hablar directamente
/// con gphoto2, PTP/IP o cualquier SDK de fabricante — solo con esta interfaz.
///
/// Implementaciones concretas (en los proyectos host, fuera de Shared):
///  · Windows  — <c>GPhoto2CameraService</c> (USB, vía libgphoto2)
///  · Android  — <c>PtpIpCameraService</c>   (WiFi, vía PTP/IP estándar)
///  · Cualquier plataforma — <c>MockCameraService</c> (simulación para pruebas / Web)
///
/// Restricción de diseño: cero dependencias de SDKs propietarios (Canon EDSDK,
/// Nikon SDK, Sony Camera Remote SDK...). La arquitectura confía únicamente en
/// estándares abiertos — libgphoto2 (que a su vez habla PTP/MTP) y PTP/IP puro
/// (ISO 15740 sobre TCP) — por lo que cualquier implementación es agnóstica
/// de fabricante.
/// </summary>
public interface ICameraService
{
    /// <summary>True si hay una cámara detectada y lista para recibir comandos.</summary>
    bool IsConnected { get; }

    /// <summary>Último error de comunicación, o null si la última operación fue exitosa.</summary>
    string? LastError { get; }

    /// <summary>
    /// Se dispara cuando cambia el estado de conexión (conectada/desconectada),
    /// permitiendo que la UI reaccione sin sondeo activo (polling).
    /// </summary>
    event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Establece conexión con la cámara: detección USB (Windows/libgphoto2)
    /// o apertura de sesión PTP/IP sobre WiFi (Android). Debe ser idempotente:
    /// llamar varias veces no debe duplicar recursos ni romper una conexión
    /// ya activa.
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica apertura, ISO y velocidad de obturación a la cámara conectada.
    /// Firma exacta requerida por el contrato — sin estos tres parámetros no
    /// hay forma de reproducir en la cámara física los valores calculados por
    /// la Calculadora de Exposición de Eclipses.
    /// </summary>
    Task SetExposureAsync(ExposureSetting settings);

    /// <summary>
    /// Dispara una toma. La implementación decide si espera confirmación de
    /// escritura en tarjeta según las capacidades del protocolo subyacente.
    /// Acepta cancelación porque puede invocarse decenas de veces seguidas
    /// dentro de una serie (ráfaga) que debe poder interrumpirse al instante
    /// si llega un nuevo evento del eclipse.
    /// </summary>
    Task TriggerCaptureAsync(CancellationToken ct);

    /// <summary>Cierra la sesión/conexión con la cámara y libera recursos de red o proceso.</summary>
    Task DisconnectAsync();
}
