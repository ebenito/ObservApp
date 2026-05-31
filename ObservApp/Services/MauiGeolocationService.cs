using Microsoft.Maui.Devices.Sensors;
using ObservApp.Shared.Services;

namespace ObservApp.Services;

/// <summary>
/// Implementación de <see cref="IGeolocationService"/> para .NET MAUI.
/// Usa <see cref="IGeolocation"/> del framework y gestiona el ciclo de
/// permisos en tiempo de ejecución.
/// </summary>
public sealed class MauiGeolocationService : IGeolocationService
{
    private readonly IGeolocation _geolocation;

    public string? LastError { get; private set; }

    public MauiGeolocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation;
    }

    public async Task<LocationData?> GetCurrentLocationAsync(
        bool highAccuracy = true,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            // ── Comprobar y solicitar permiso ─────────────────────────────
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                LastError = "Permiso de ubicación denegado.";
                return null;
            }

            // ── Obtener ubicación ─────────────────────────────────────────
            var request = new GeolocationRequest(
                highAccuracy ? GeolocationAccuracy.High : GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(15));

            var location = await _geolocation.GetLocationAsync(request, cancellationToken);

            if (location is null)
            {
                LastError = "No se pudo obtener la ubicación del dispositivo.";
                return null;
            }

            var source = location.IsFromMockProvider ? "Mock" :
                         highAccuracy ? "GPS" : "Red/WiFi";

            return new LocationData(
                Latitude: location.Latitude,
                Longitude: location.Longitude,
                AltitudeMeters: location.Altitude,
                AccuracyMeters: location.Accuracy,
                SourceLabel: source);
        }
        catch (FeatureNotSupportedException)
        {
            LastError = "El GPS no está disponible en este dispositivo.";
            return null;
        }
        catch (FeatureNotEnabledException)
        {
            LastError = "La ubicación está desactivada en el dispositivo. Actívala en Ajustes.";
            return null;
        }
        catch (PermissionException)
        {
            LastError = "Permiso de ubicación denegado.";
            return null;
        }
        catch (OperationCanceledException)
        {
            LastError = "Solicitud de ubicación cancelada.";
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"Error al obtener la ubicación: {ex.Message}";
            return null;
        }
    }
}
