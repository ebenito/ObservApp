using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ObservApp.Services;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using Syncfusion.Blazor;

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

        // ── HttpClient ────────────────────────────────────────────────────────
        // Configurar HttpClient con User-Agent válido para que RSS feeds no rechacen las requests
        builder.Services.AddHttpClient("default", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/atom+xml, application/xml, text/xml, */*");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

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
			var astrobit   = new RssSource("astrobit","Astrobitácora",     "https://www.astrobitacora.com/feed/",	            IsBuiltIn: true);
			var astrobites = new RssSource(	"astrobites", "Astrobites ES", "https://astrobitos.org/feed/",                      IsBuiltIn: true);
			var esaes      = new RssSource("esaes", "ESA España",          "https://www.esa.int/rssfeed/Spain",	                IsBuiltIn: true);
			var nasaes     = new RssSource("nasa", "Universo curioso de la NASA", "https://feeds.megaphone.fm/nationalaeronauticsandspaceadministration5412631684", IsBuiltIn: true);
			//var eureka = new RssSource("eureka", "Eureka", "https://danielmarin.naukas.com/feed/", IsBuiltIn: true);
			//var naukas = new RssSource("naukas", "Naukas", "https://naukas.com/feed/", IsBuiltIn: true);
			//var csic = new RssSource("csic", "CSIC", "https://www.csic.es/es/actualidad-del-csic/rss.xml", IsBuiltIn: true);
			//var muyint = new RssSource("muyint", "Muy Interesante", "https://www.muyinteresante.com/feed/", IsBuiltIn: true);
			// ── Fuentes RSS — Inglés ──────────────────────────────────────────
            var nasa     = new RssSource("nasa",     "NASA",                  "https://www.nasa.gov/feed/",                               IsBuiltIn: true);
			var nasaimg  = new RssSource("nasa",     "NASA Image of the Day", "https://www.nasa.gov/feeds/iotd-feed/",                    IsBuiltIn: true);
            var esa      = new RssSource("esa",      "ESA",                   "http://www.esa.int/rssfeed/Our_Activities/Space_Science",  IsBuiltIn: true);
            var eso      = new RssSource("eso",      "ESO",                   "https://www.eso.org/public/blog/feed/",                    IsBuiltIn: true);
            var skytel   = new RssSource("skytel",   "Sky & Telescope",       "https://skyandtelescope.org/feed/",                        IsBuiltIn: true);
            var astromag = new RssSource("astromag", "Astronomy Magazine",    "https://www.astronomy.com/feed/",                          IsBuiltIn: true);

            var rssProviders = new[]
            {
                new RssFeedArticleProvider(rss, astrobit,     languageCode: "es"),
                new RssFeedArticleProvider(rss, astrobites,   languageCode: "es"),
                new RssFeedArticleProvider(rss, esaes,        languageCode: "es"),
				new RssFeedArticleProvider(rss, nasaes,       languageCode: "es"),
				new RssFeedArticleProvider(rss, nasa,     languageCode: "en"),
                new RssFeedArticleProvider(rss, nasaimg,  languageCode: "en"),
                new RssFeedArticleProvider(rss, esa,      languageCode: "en"),
                new RssFeedArticleProvider(rss, eso,      languageCode: "en"),
                new RssFeedArticleProvider(rss, skytel,   languageCode: "en"),
                new RssFeedArticleProvider(rss, astromag, languageCode: "en"),
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
