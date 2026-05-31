using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Implementación de <see cref="IGeolocationService"/> para WebAssembly.
/// Usa la API <c>navigator.geolocation</c> del navegador a través de JSInterop.
/// </summary>
public sealed class WebGeolocationService : IGeolocationService
{
    private readonly IJSRuntime _js;

    public string? LastError { get; private set; }

    public WebGeolocationService(IJSRuntime js)
    {
        _js = js;
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
                LastError = "No se pudo obtener la ubicación del navegador.";
                return null;
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                LastError = result.Error;
                return null;
            }

            return new LocationData(
                Latitude: result.Latitude,
                Longitude: result.Longitude,
                AltitudeMeters: result.Altitude,
                AccuracyMeters: result.Accuracy,
                SourceLabel: "Navegador");
        }
        catch (JSException ex)
        {
            LastError = $"Error del navegador: {ex.Message}";
            return null;
        }
        catch (OperationCanceledException)
        {
            LastError = "Solicitud de ubicación cancelada.";
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"Error inesperado: {ex.Message}";
            return null;
        }
    }

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
