using ObservApp.Shared.Services;

namespace ObservApp.Services;

/// <summary>
/// Implementación de <see cref="IExternalLinkService"/> para .NET MAUI.
/// Usa <see cref="Launcher"/> para delegar la apertura de la URL al
/// navegador del sistema mediante un intent nativo. Esto evita el
/// fallo de "window.open" dentro del BlazorWebView de Android, que
/// no implementa WebChromeClient.onCreateWindow por defecto.
/// </summary>
public sealed class MauiExternalLinkService : IExternalLinkService
{
    public async Task OpenAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch
        {
            // Si no hay app capaz de abrir la URL, no rompemos la app.
        }
    }
}