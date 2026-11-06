using Microsoft.Extensions.Localization;
using Microsoft.Maui.Devices.Sensors;
using ObservApp.Shared.Services;

namespace ObservApp.Services;

/// <summary>
/// Implementación de <see cref="IGeolocationService"/> para .NET MAUI.
/// Usa <see cref="IGeolocation"/> del framework y gestiona el ciclo de
/// permisos en tiempo de ejecución. Los mensajes de error salen de las
/// claves <c>Gps_Error_*</c> del sistema de traducciones.
/// </summary>
public sealed class MauiGeolocationService : IGeolocationService
{
    private readonly IGeolocation _geolocation;
    private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

    public string? LastError { get; private set; }

    public MauiGeolocationService(IGeolocation geolocation, IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
    {
        _geolocation = geolocation;
        _l = localizer;
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
                LastError = _l["Gps_Error_PermissionDenied"];
                return null;
            }

            // ── Obtener ubicación ─────────────────────────────────────────
            var request = new GeolocationRequest(
                highAccuracy ? GeolocationAccuracy.High : GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(15));

            var location = await _geolocation.GetLocationAsync(request, cancellationToken);

            if (location is null)
            {
                LastError = _l["Gps_Error_Unavailable"];
                return null;
            }

            var source = location.IsFromMockProvider ? _l["Gps_Source_Mock"].Value :
                         highAccuracy ? "GPS" : _l["Gps_Source_Network"].Value;

            return new LocationData(
                Latitude: location.Latitude,
                Longitude: location.Longitude,
                AltitudeMeters: location.Altitude,
                AccuracyMeters: location.Accuracy,
                SourceLabel: source);
        }
        catch (FeatureNotSupportedException)
        {
            LastError = _l["Gps_Error_NotSupported"];
            return null;
        }
        catch (FeatureNotEnabledException)
        {
            LastError = _l["Gps_Error_Disabled"];
            return null;
        }
        catch (PermissionException)
        {
            LastError = _l["Gps_Error_PermissionDenied"];
            return null;
        }
        catch (OperationCanceledException)
        {
            LastError = _l["Gps_Error_Cancelled"];
            return null;
        }
        catch (Exception ex)
        {
            LastError = _l["Gps_Error_Generic", ex.Message];
            return null;
        }
    }
}
