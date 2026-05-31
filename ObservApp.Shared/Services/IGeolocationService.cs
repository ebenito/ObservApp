namespace ObservApp.Shared.Services;

/// <summary>
/// Datos de ubicación devueltos por el servicio de geolocalización.
/// </summary>
public record LocationData(
    double Latitude,
    double Longitude,
    double? AltitudeMeters,
    double? AccuracyMeters,
    string SourceLabel);

/// <summary>
/// Servicio reutilizable para obtener la ubicación actual del dispositivo.
/// </summary>
public interface IGeolocationService
{
    /// <summary>
    /// Solicita la ubicación actual. Devuelve null si el permiso fue denegado
    /// o si el hardware no está disponible.
    /// </summary>
    /// <param name="highAccuracy">
    /// true = GPS (más lento, más preciso); false = red/WiFi (rápido, menos preciso).
    /// </param>
    Task<LocationData?> GetCurrentLocationAsync(bool highAccuracy = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Último error ocurrido, o null si la última llamada fue exitosa.
    /// </summary>
    string? LastError { get; }
}
