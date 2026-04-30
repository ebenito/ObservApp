using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

// TODO: Reemplazar con implementación basada en localStorage via JSInterop
// para que las preferencias persistan entre sesiones web.
// Véase: https://github.com/ebenito/ObservApp/issues/1
public class WebSettingsService : ISettingsService
{
    private readonly Dictionary<string, string> _store = new()
    {
        ["app_language"] = "es",
        ["app_theme"] = "dark",
    };

    public string GetLanguage() => _store["app_language"];
    public void SetLanguage(string languageCode) => _store["app_language"] = languageCode;
    public string GetTheme() => _store["app_theme"];
    public void SetTheme(string theme) => _store["app_theme"] = theme;
}
