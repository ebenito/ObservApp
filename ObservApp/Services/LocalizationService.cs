using System.Globalization;
using Microsoft.Maui.Storage;

namespace ObservApp.Services;

/// <summary>
/// Servicio para gestionar el idioma de la aplicación en tiempo de ejecución.
/// Vive en el proyecto MAUI porque usa Preferences (API nativa).
/// </summary>
public class LocalizationService
{
	public static readonly IReadOnlyList<LanguageOption> SupportedLanguages = new List<LanguageOption>
	{
		new("es", "Español", "🇪🇸"),
		new("en", "English", "🇬🇧"),
	};

	public event Action? OnLanguageChanged;

	public CultureInfo CurrentCulture { get; private set; } =
		CultureInfo.CurrentCulture;

	public string CurrentLanguageCode => CurrentCulture.TwoLetterISOLanguageName;

	public LocalizationService()
	{
		var saved = Preferences.Default.Get("app_language", string.Empty);
		ApplyCulture(
			!string.IsNullOrEmpty(saved) ? saved : GetDefaultLanguageCode(),
			notify: false);
	}

	public void SetLanguage(string languageCode)
	{
		if (languageCode == CurrentLanguageCode) return;
		ApplyCulture(languageCode, notify: true);
		Preferences.Default.Set("app_language", languageCode);
	}

	private void ApplyCulture(string languageCode, bool notify)
	{
		var supported = SupportedLanguages.FirstOrDefault(l => l.Code == languageCode);
		if (supported is null) return;

		var culture = new CultureInfo(languageCode);
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		CurrentCulture = culture;

		if (notify)
			OnLanguageChanged?.Invoke();
	}

	private static string GetDefaultLanguageCode()
	{
		var systemLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
		return SupportedLanguages.Any(l => l.Code == systemLang) ? systemLang : "es";
	}
}

public record LanguageOption(string Code, string Name, string Flag);