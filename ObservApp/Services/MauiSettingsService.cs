using Microsoft.Maui.Storage;
using ObservApp.Shared.Services;

namespace ObservApp.Services;

public class MauiSettingsService : ISettingsService
{
    private const string LanguageKey = "app_language";
    private const string ThemeKey = "app_theme";

    public string GetLanguage() =>
        Preferences.Default.Get(LanguageKey, "es");

    public void SetLanguage(string languageCode) =>
        Preferences.Default.Set(LanguageKey, languageCode);

    public string GetTheme() =>
        Preferences.Default.Get(ThemeKey, "dark");

    public void SetTheme(string theme) =>
        Preferences.Default.Set(ThemeKey, theme);
}
