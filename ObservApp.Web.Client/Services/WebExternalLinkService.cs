using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Implementación de <see cref="IExternalLinkService"/> para Blazor WebAssembly.
/// Abre la URL en una nueva pestaña del navegador mediante window.open.
/// </summary>
public sealed class WebExternalLinkService : IExternalLinkService
{
    private readonly IJSRuntime _js;

    public WebExternalLinkService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task OpenAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out _)) return;

        try
        {
            await _js.InvokeVoidAsync("open", url, "_blank");
        }
        catch
        {
            // Silencio — no debe romper la UI si el popup es bloqueado.
        }
    }
}