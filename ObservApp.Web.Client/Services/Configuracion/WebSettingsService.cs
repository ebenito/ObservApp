using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

// Implementación con localStorage via JSInterop.
// Resuelve el issue #1: las preferencias ahora persisten entre recargas.
public class WebSettingsService : ISettingsService
{
    private readonly IJSRuntime _js;

    // Caché en memoria para lecturas síncronas (ISettingsService es síncrono)
    private string _language = "es";
    private string _theme = "dark";
    private bool _initialized;

    public WebSettingsService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Llama a este método una vez al arrancar (desde Program.cs o App.razor)
    /// para leer los valores de localStorage antes de usarlos.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var lang = await _js.InvokeAsync<string?>("localStorage.getItem", "app_language");
            var theme = await _js.InvokeAsync<string?>("localStorage.getItem", "app_theme");

            if (!string.IsNullOrEmpty(lang)) _language = lang;
            if (!string.IsNullOrEmpty(theme)) _theme = theme;
        }
        catch
        {
            // localStorage no disponible (SSR prerender) — usar defaults
        }

        _initialized = true;
    }

    public string GetLanguage() => _language;
    public string GetTheme() => _theme;

    public void SetLanguage(string languageCode)
    {
        _language = languageCode;
        _ = _js.InvokeVoidAsync("localStorage.setItem", "app_language", languageCode);
    }

    public void SetTheme(string theme)
    {
        _theme = theme;
        _ = _js.InvokeVoidAsync("localStorage.setItem", "app_theme", theme);
    }
}