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
            new("de", "Deutsch",  "🇩🇪"),
            new("it", "Italiano", "🇮🇹"),
            new("ar", "العربية",  "🇸🇦"),
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
    public event Action? OnLanguageChanged;

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
        // Persiste en localStorage (clave compatible con loading-texts.js)
        await _js.InvokeVoidAsync("ObservApp.applyLang",
            languageCode,
            languageCode == "ar" ? "rtl" : "ltr");

        // Recarga la página — Blazor WASM leerá la cultura en Program.cs
        await _js.InvokeVoidAsync("location.reload");
    }
}