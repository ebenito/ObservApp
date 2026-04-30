using System.Globalization;

namespace ObservApp.Shared.Services;

public interface ILocalizationService
{
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }
    CultureInfo CurrentCulture { get; }
    string CurrentLanguageCode { get; }
    event Action? OnLanguageChanged;
    void SetLanguage(string languageCode);
}

public record LanguageOption(string Code, string Name, string Flag);
