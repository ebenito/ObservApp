using System.Globalization;
using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

public class WebLocalizationService : ILocalizationService
{
    private static readonly IReadOnlyList<LanguageOption> _supportedLanguages =
        new List<LanguageOption>
        {
            new("es", "Español",  "🇪🇸"),
            new("en", "English",  "🇬🇧"),
            new("fr", "Français", "🇫🇷"),
            new("pt", "Português", "🇵🇹"),
            new("de", "Deutsch",  "🇩🇪"),
            new("it", "Italiano", "🇮🇹"),
            new("ar", "العربية",  "🇸🇦"),
            new("ru", "Русский",  "🇷🇺"),
        };

    private readonly IJSRuntime _js;

    public WebLocalizationService(IJSRuntime js)
    {
        _js = js;

        // La cultura ya viene establecida por Program.cs al arrancar,
        // así que simplemente reflejamos la cultura activa.
        CurrentCulture = CultureInfo.CurrentUICulture;
    }

    public IReadOnlyList<LanguageOption> SupportedLanguages => _supportedLanguages;
    public CultureInfo CurrentCulture { get; private set; }
    public string CurrentLanguageCode => CurrentCulture.TwoLetterISOLanguageName;

    // En WASM el cambio de idioma requiere recarga de página.
    // El evento no se dispara — la recarga sustituye al re-render.
#pragma warning disable CS0067 // Lo exige ILocalizationService; este host no lo emite
    public event Action? OnLanguageChanged;
#pragma warning restore CS0067

    public void SetLanguage(string languageCode)
    {
        if (languageCode == CurrentLanguageCode) return;

        var supported = _supportedLanguages.FirstOrDefault(l => l.Code == languageCode);
        if (supported is null) return;

        // Guardar en localStorage y recargar — es el mecanismo oficial de Blazor WASM
        _ = SetLanguageAndReloadAsync(languageCode);
    }

    private async Task SetLanguageAndReloadAsync(string languageCode)
    {
        // Persiste en localStorage y recarga la página desde JS con su contexto nativo
        await _js.InvokeVoidAsync("ObservApp.setLangAndReload",
            languageCode,
            languageCode == "ar" ? "rtl" : "ltr");
    }
}