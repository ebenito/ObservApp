using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Implementación de <see cref="IGeolocationService"/> para WebAssembly.
/// Usa la API <c>navigator.geolocation</c> del navegador a través de JSInterop.
/// El JS (<c>geolocation.js</c>) devuelve códigos de error, no textos; aquí se
/// traducen con las claves <c>Gps_Error_*</c>.
/// </summary>
public sealed class WebGeolocationService : IGeolocationService
{
    private readonly IJSRuntime _js;
    private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

    public string? LastError { get; private set; }

    public WebGeolocationService(IJSRuntime js, IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
    {
        _js = js;
        _l = localizer;
    }

    public async Task<LocationData?> GetCurrentLocationAsync(
        bool highAccuracy = true,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        try
        {
            var result = await _js.InvokeAsync<BrowserLocation?>(
                "observApp.getCurrentPosition",
                cancellationToken,
                new object[] { highAccuracy });

            if (result is null)
            {
                LastError = _l["Gps_Error_Unavailable"];
                return null;
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                LastError = _l[MapBrowserErrorKey(result.Error)];
                return null;
            }

            return new LocationData(
                Latitude: result.Latitude,
                Longitude: result.Longitude,
                AltitudeMeters: result.Altitude,
                AccuracyMeters: result.Accuracy,
                SourceLabel: _l["Gps_Source_Browser"].Value);
        }
        catch (JSException ex)
        {
            LastError = _l["Gps_Error_Browser", ex.Message];
            return null;
        }
        catch (OperationCanceledException)
        {
            LastError = _l["Gps_Error_Cancelled"];
            return null;
        }
        catch (Exception ex)
        {
            LastError = _l["Error_Unexpected", ex.Message];
            return null;
        }
    }

    /// <summary>Códigos devueltos por geolocation.js → clave de recurso.</summary>
    private static string MapBrowserErrorKey(string code) => code switch
    {
        "unsupported" => "Gps_Error_BrowserUnsupported",
        "permission_denied" => "Gps_Error_PermissionDenied",
        "position_unavailable" => "Gps_Error_Unavailable",
        "timeout" => "Gps_Error_Timeout",
        _ => "Gps_Error_Unavailable",
    };

    // Modelo interno que mapea el objeto JS devuelto
    private sealed class BrowserLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public double? Accuracy { get; set; }
        public string? Error { get; set; }
    }
}
