using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public sealed class WebEclipseAudioService : IEclipseAudioService
{
    private readonly IJSRuntime _js;

    public WebEclipseAudioService(IJSRuntime js) => _js = js;

    public bool IsSupported => true;

    public async Task AnnounceEventAsync(string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _js.InvokeVoidAsync("observApp.speakText",
                cancellationToken,
                new object[] { message });
        }
        catch { /* silencio */ }
    }

    public async Task PlayBeepAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _js.InvokeVoidAsync("observApp.playBeepTone",
                cancellationToken,
                Array.Empty<object>());
        }
        catch { /* silencio */ }
    }
}