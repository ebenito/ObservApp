using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public sealed class WebEclipseAudioService : IEclipseAudioService
{
    private readonly IJSRuntime _js;

    public WebEclipseAudioService(IJSRuntime js) => _js = js;

    public bool IsSupported => true;

    public Task AnnounceEventAsync(string message,
        CancellationToken cancellationToken = default)
        // En web solo reproducimos el beep (sin TTS)
        => PlayBeepAsync(cancellationToken);

    public async Task PlayBeepAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _js.InvokeVoidAsync("observApp.playBeep",
                cancellationToken, "_content/ObservApp.Shared/sounds/eclipse-beep.wav");
        }
        catch { /* silencio si el navegador bloquea audio */ }
    }
}