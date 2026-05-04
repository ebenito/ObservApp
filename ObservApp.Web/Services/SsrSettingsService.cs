using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

public class SsrSettingsService : ISettingsService
{
    public string GetLanguage() => "es";
    public string GetTheme() => "dark";
    public void SetLanguage(string languageCode) { /* no-op en SSR */ }
    public void SetTheme(string theme) { /* no-op en SSR */ }
}