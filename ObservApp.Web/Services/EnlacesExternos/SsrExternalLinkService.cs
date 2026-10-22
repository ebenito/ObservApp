using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

/// <summary>
/// Stub de <see cref="IExternalLinkService"/> para el lado servidor (SSR).
/// No-op: en el render inicial del servidor no hay contexto de navegador
/// disponible para abrir enlaces. La interacción real ocurre tras la
/// hidratación en el cliente WASM (<see cref="ObservApp.Web.Client.Services.WebExternalLinkService"/>).
/// </summary>
public sealed class SsrExternalLinkService : IExternalLinkService
{
    public Task OpenAsync(string url) => Task.CompletedTask;
}
