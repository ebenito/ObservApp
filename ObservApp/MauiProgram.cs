using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ObservApp.Services;
using ObservApp.Shared.Interfaces;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using Syncfusion.Blazor;
using System.Reflection;

namespace ObservApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // ── Configuración — EmbeddedResource (funciona en Android y Windows) ─
        // appsettings.json        → commiteado, sin claves reales
        // appsettings.Local.json  → en .gitignore, con claves reales (local y CI)
        var assembly = Assembly.GetExecutingAssembly();

        using (var stream = assembly.GetManifestResourceStream("ObservApp.appsettings.json"))
            if (stream != null)
                builder.Configuration.AddJsonStream(stream);

        using (var stream = assembly.GetManifestResourceStream("ObservApp.appsettings.Local.json"))
            if (stream != null)
                builder.Configuration.AddJsonStream(stream);

        // ── Licencia Syncfusion ──────────────────────────────────────────────
        var syncfusionKey =
            builder.Configuration["SYNCFUSION_LICENSE_KEY"] ??
            builder.Configuration["SyncfusionLicenseKey"] ??
            string.Empty;

        if (!string.IsNullOrEmpty(syncfusionKey))
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Blazor WebView ───────────────────────────────────────────────────
        builder.Services.AddMauiBlazorWebView();

        // ── Syncfusion ───────────────────────────────────────────────────────
        builder.Services.AddSyncfusionBlazor();

        // ── Localización ─────────────────────────────────────────────────────
        builder.Services.AddLocalization();
        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddSingleton<ILocalizationService>(
            sp => sp.GetRequiredService<LocalizationService>());
        builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
        builder.Services.AddSingleton<IAppLifecycleService, MauiAppLifecycleService>();
        builder.Services.AddSingleton<AppState>();

        // ── Geolocalización ───────────────────────────────────────────────────
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IGeolocationService, MauiGeolocationService>();

        // ── Ubicaciones Favoritas ─────────────────────────────────────────────
        builder.Services.AddSingleton<IFavoriteLocationsService, MauiFavoriteLocationsService>();

        // ── Calculadora de tiempos de eclipses ────────────────────────────────
        builder.Services.AddSingleton<IEclipseCalculatorService, EclipseCalculatorService>();
        builder.Services.AddSingleton<IEclipseAudioService, MauiEclipseAudioService>();

        // ── Servicio de lectura de fuentes RSS (parser base) ─────────────────
        builder.Services.AddSingleton<IRssFeedService, RssFeedService>();

        // ── Enlaces externos (abrir en navegador del sistema) ─────────────────
        builder.Services.AddSingleton<IExternalLinkService, MauiExternalLinkService>();

        // ── TTS genérico por idioma (lectura de artículos en Señales) ─────────
        builder.Services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();

		// ── Servicio de disparo de fotografías automatizadas (Android y Windows) ─────────────
		builder.Services.AddSingleton<EclipseCameraProfileState>();
#if WINDOWS
        builder.Services.AddSingleton<ICameraService, GPhoto2CameraService>();
#elif ANDROID
		builder.Services.AddSingleton<ICameraService, PtpIpCameraService>();
#endif
		builder.Services.AddSingleton<CameraManager>();

		// ── HttpClient ────────────────────────────────────────────────────────
		builder.Services.AddHttpClient();

        // ── Servicio unificado de artículos (WP API + fuentes RSS) ───────────
        builder.Services.AddSingleton<IArticleService>(sp =>
        {
            var http = sp.GetRequiredService<HttpClient>();
            var rss  = (RssFeedService)sp.GetRequiredService<IRssFeedService>();

            var wpProvider = new WpRestArticleProvider(http,
                baseUrl: "https://tubkala.com",
                sourceName: "Tubkala",
                languageCode: "es");

            // ── Fuentes RSS — Español ─────────────────────────────────────────
            var sinc     = new RssSource("sinc",     "SINC",                  "https://www.agenciasinc.es/rss/feed/es_ES/noticias/astronomia", IsBuiltIn: true);
            var iac      = new RssSource("iac",      "IAC",                   "https://www.iac.es/es/rss.xml",                                 IsBuiltIn: true);
            var iyc      = new RssSource("iyc",      "Invest. y Ciencia",     "https://www.investigacionyciencia.es/noticias/rss",              IsBuiltIn: true);
            var astropaf = new RssSource("astropaf", "Astropaf",              "https://astropaf.com/feed/",                                    IsBuiltIn: true);
            // ── Fuentes RSS — Inglés ──────────────────────────────────────────
            var jpl      = new RssSource("jpl",      "NASA JPL",              "https://www.jpl.nasa.gov/feeds/news/",                          IsBuiltIn: true);
            var esa      = new RssSource("esa",      "ESA",                   "https://www.esa.int/rssfeed/RSSFeed/1/Astronomy",               IsBuiltIn: true);
            var eso      = new RssSource("eso",      "ESO",                   "https://www.eso.org/public/outreach/rss/news/",                 IsBuiltIn: true);
            var skytel   = new RssSource("skytel",   "Sky & Telescope",       "https://skyandtelescope.org/feed/",                            IsBuiltIn: true);
            var astromag = new RssSource("astromag", "Astronomy Magazine",    "https://www.astronomy.com/feed/",                              IsBuiltIn: true);
            var spacecom = new RssSource("spacecom", "Space.com",             "https://www.space.com/feeds/all",                              IsBuiltIn: true);

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

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // TODO: registrar servicios a medida que se vayan creando:
        // builder.Services.AddSingleton<SupabaseService>();
        // builder.Services.AddSingleton<AuthService>();

        return builder.Build();
    }
}
