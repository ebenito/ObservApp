using System.Globalization;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Services;

public class SsrLocalizationService : ILocalizationService
{
    private static readonly IReadOnlyList<LanguageOption> _supportedLanguages =
    [
        new("es", "Español",  "🇪🇸"),
        new("en", "English",  "🇬🇧"),
        new("fr", "Français", "🇫🇷"),
        new("de", "Deutsch",  "🇩🇪"),
        new("it", "Italiano", "🇮🇹"),
        new("ar", "العربية",  "🇸🇦"),
    ];

    public IReadOnlyList<LanguageOption> SupportedLanguages => _supportedLanguages;
    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;
    public string CurrentLanguageCode => CurrentCulture.TwoLetterISOLanguageName;
    public event Action? OnLanguageChanged;
    public void SetLanguage(string languageCode) { /* no-op en SSR */ }
}