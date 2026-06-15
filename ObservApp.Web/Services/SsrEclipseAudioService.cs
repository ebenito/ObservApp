using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

/// <summary>
/// Implementación SSR de <see cref="IEclipseAudioService"/>.
/// En el servidor no hay soporte para audio, por lo que esta es una implementación vacía (no-op).
/// </summary>
public sealed class SsrEclipseAudioService : IEclipseAudioService
{
    public bool IsSupported => false;

    public Task AnnounceEventAsync(string message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PlayBeepAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
