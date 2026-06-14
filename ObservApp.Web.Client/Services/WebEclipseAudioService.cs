using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public sealed class WebEclipseAudioService : IEclipseAudioService
{
    private readonly IJSRuntime _js;

    // Ruta base sin extensión — el JS prueba .mp3 luego .wav automáticamente
    private const string AudioFallbackUrl =
        "_content/ObservApp.Shared/sounds/eclipse-beep";

    public WebEclipseAudioService(IJSRuntime js) => _js = js;

    public bool IsSupported => true;

    public async Task AnnounceEventAsync(string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _js.InvokeVoidAsync(
                "observApp.speakTextWithFallback",
                cancellationToken,
                new object[] { message, AudioFallbackUrl });
        }
        catch { /* silencio */ }
    }

    public async Task PlayBeepAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _js.InvokeVoidAsync(
                "observApp.playBeepTone",
                cancellationToken,
                Array.Empty<object>());
        }
        catch { /* silencio */ }
    }
}