using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Web.Client.Services;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

#if STANDALONE_WASM
// Solo activo en el build standalone para Azure Static Web Apps.
// En modo hosted (local + MAUI) el servidor registra los root components
// vía MapRazorComponents<App>() — añadirlos aquí también causaría conflicto.
builder.RootComponents.Add<ObservApp.Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
#endif

// ── Licencia Syncfusion ──────────────────────────────────────────────────────
var syncfusionKey =
    builder.Configuration["SYNCFUSION_LICENSE_KEY"] ??
    builder.Configuration["SyncfusionLicenseKey"] ??
    string.Empty;

if (!string.IsNullOrEmpty(syncfusionKey))
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

builder.Services.AddSyncfusionBlazor();
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, WebLocalizationService>();
builder.Services.AddSingleton<ISettingsService, WebSettingsService>();
builder.Services.AddSingleton<IAppLifecycleService, WebClientAppLifecycleService>();
builder.Services.AddSingleton<IGeolocationService, WebGeolocationService>();
builder.Services.AddSingleton<IFavoriteLocationsService, WebFavoriteLocationsService>();
builder.Services.AddSingleton<IExternalLinkService, WebExternalLinkService>();
builder.Services.AddSingleton<ITextToSpeechService, WebTextToSpeechService>();
builder.Services.AddSingleton<IEclipseCalculatorService, EclipseCalculatorService>();
builder.Services.AddSingleton<IEclipseAudioService, WebEclipseAudioService>();
builder.Services.AddSingleton<AppState>();

// ── IRssFeedService vía proxy SSR (evita CORS en WASM) ──────────────────────
builder.Services.AddHttpClient<IRssFeedService, WebRssFeedService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// ── IArticleService: WP API directa + RSS vía proxy SSR ─────────────────────
// WpRestArticleProvider llama a tubkala.com directamente (CORS OK confirmado).
// RssFeedArticleProvider usa WebRssFeedService que enruta por el proxy SSR.
builder.Services.AddHttpClient("WpDirect");

builder.Services.AddSingleton<IArticleService>(sp =>
{
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var wpHttp = httpFactory.CreateClient("WpDirect");
    var rss    = (RssFeedService)sp.GetRequiredService<IRssFeedService>();

    var wpProvider = new WpRestArticleProvider(wpHttp,
        baseUrl: "https://tubkala.com/",
        sourceName: "Tubkala",
        languageCode: "es");

    // ── Fuentes RSS — Español ─────────────────────────────────────────────────
    var sinc     = new RssSource("sinc",     "SINC",               "https://www.agenciasinc.es/rss/feed/es_ES/noticias/astronomia", IsBuiltIn: true);
    var iac      = new RssSource("iac",      "IAC",                "https://www.iac.es/es/rss.xml",                                 IsBuiltIn: true);
    var iyc      = new RssSource("iyc",      "Invest. y Ciencia",  "https://www.investigacionyciencia.es/noticias/rss",              IsBuiltIn: true);
    var astropaf = new RssSource("astropaf", "Astropaf",           "https://astropaf.com/feed/",                                    IsBuiltIn: true);
    // ── Fuentes RSS — Inglés ──────────────────────────────────────────────────
    var jpl      = new RssSource("jpl",      "NASA JPL",           "https://www.jpl.nasa.gov/feeds/news/",                          IsBuiltIn: true);
    var esa      = new RssSource("esa",      "ESA",                "https://www.esa.int/rssfeed/RSSFeed/1/Astronomy",               IsBuiltIn: true);
    var eso      = new RssSource("eso",      "ESO",                "https://www.eso.org/public/outreach/rss/news/",                 IsBuiltIn: true);
    var skytel   = new RssSource("skytel",   "Sky & Telescope",    "https://skyandtelescope.org/feed/",                            IsBuiltIn: true);
    var astromag = new RssSource("astromag", "Astronomy Magazine", "https://www.astronomy.com/feed/",                              IsBuiltIn: true);
    var spacecom = new RssSource("spacecom", "Space.com",          "https://www.space.com/feeds/all",                              IsBuiltIn: true);

    var rssProviders = new[]
    {
        new RssFeedArticleProvider(rss, sinc,     languageCode: "es"),
        new RssFeedArticleProvider(rss, iac,      languageCode: "es"),
        new RssFeedArticleProvider(rss, iyc,      languageCode: "es"),
        new RssFeedArticleProvider(rss, astropaf, languageCode: "es"),
        new RssFeedArticleProvider(rss, jpl,      languageCode: "en"),
        new RssFeedArticleProvider(rss, esa,      languageCode: "en"),
        new RssFeedArticleProvider(rss, eso,      languageCode: "en"),
        new RssFeedArticleProvider(rss, skytel,   languageCode: "en"),
        new RssFeedArticleProvider(rss, astromag, languageCode: "en"),
        new RssFeedArticleProvider(rss, spacecom, languageCode: "en"),
    };

    return new ArticleService(wpProvider, rssProviders);
});

var host = builder.Build();

var settingsService = host.Services.GetRequiredService<ISettingsService>();
if (settingsService is WebSettingsService webSettings)
    await webSettings.InitializeAsync();

var savedLang = settingsService.GetLanguage();
var culture = new CultureInfo(savedLang);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
