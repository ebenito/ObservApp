using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

/// <summary>
/// Stub de <see cref="IGeolocationService"/> para el lado servidor (SSR).
/// La geolocalización real solo está disponible en el cliente (WASM / MAUI).
/// </summary>
public sealed class SsrGeolocationService : IGeolocationService
{
    private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

    public SsrGeolocationService(IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
    {
        _l = localizer;
    }

    public string? LastError => _l["Gps_Error_ServerUnavailable"];

    public Task<LocationData?> GetCurrentLocationAsync(
        bool highAccuracy = true,
        CancellationToken cancellationToken = default)
        => Task.FromResult<LocationData?>(null);
}
