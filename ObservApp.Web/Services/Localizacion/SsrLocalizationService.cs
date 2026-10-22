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
        new("pt", "Português", "🇵🇹"),
        new("de", "Deutsch",  "🇩🇪"),
        new("it", "Italiano", "🇮🇹"),
        new("ar", "العربية",  "🇸🇦"),
        new("ru", "Русский",  "🇷🇺"),
    ];

    public IReadOnlyList<LanguageOption> SupportedLanguages => _supportedLanguages;
    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;
    public string CurrentLanguageCode => CurrentCulture.TwoLetterISOLanguageName;
#pragma warning disable CS0067 // Lo exige ILocalizationService; el stub SSR no lo emite
    public event Action? OnLanguageChanged;
#pragma warning restore CS0067
    public void SetLanguage(string languageCode) { /* no-op en SSR */ }
}