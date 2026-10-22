using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

/// <summary>
/// Stub de <see cref="IGeolocationService"/> para el lado servidor (SSR).
/// La geolocalización real solo está disponible en el cliente (WASM / MAUI).
/// </summary>
public sealed class SsrGeolocationService : IGeolocationService
{
    public string? LastError => "La geolocalización no está disponible en el servidor.";

    public Task<LocationData?> GetCurrentLocationAsync(
        bool highAccuracy = true,
        CancellationToken cancellationToken = default)
        => Task.FromResult<LocationData?>(null);
}
