using System.Globalization;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public class WebLocalizationService : ILocalizationService
{
    private static readonly IReadOnlyList<LanguageOption> _supportedLanguages = new List<LanguageOption>
    {
        new("es", "Español",  "🇪🇸"),
        new("en", "English",  "🇬🇧"),
        new("fr", "Français", "🇫🇷"),
        new("de", "Deutsch",  "🇩🇪"),
        new("it", "Italiano", "🇮🇹"),
        new("ar", "العربية",  "🇸🇦"),
    };

    public IReadOnlyList<LanguageOption> SupportedLanguages => _supportedLanguages;

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentCulture;

    public string CurrentLanguageCode => CurrentCulture.TwoLetterISOLanguageName;

    public event Action? OnLanguageChanged;

    public void SetLanguage(string languageCode)
    {
        if (languageCode == CurrentLanguageCode) return;

        var supported = _supportedLanguages.FirstOrDefault(l => l.Code == languageCode);
        if (supported is null) return;

        var culture = new CultureInfo(languageCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CurrentCulture = culture;

        OnLanguageChanged?.Invoke();
    }
}
