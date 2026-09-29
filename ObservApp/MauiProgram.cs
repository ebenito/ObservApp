using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ObservApp.Services;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Shared.ViewModels;
using Supabase;
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
        builder.Services.AddSingleton<ILocalizationService>(sp => sp.GetRequiredService<LocalizationService>());
        builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
        builder.Services.AddSingleton<INavigationHistoryService, NavigationHistoryService>();
        builder.Services.AddSingleton<IAppLifecycleService, MauiAppLifecycleService>();
        builder.Services.AddSingleton<IVersionService, VersionService>();
        builder.Services.AddSingleton<AppState>();

        // ── Autenticación y persistencia con Supabase ──────────────────────────
        var supabaseUrl = (builder.Configuration["SupabaseUrl"] ?? string.Empty).Trim();
        var supabaseKey = (builder.Configuration["SupabaseAnonKey"] ?? string.Empty).Trim();

        builder.Services.AddSingleton(sp =>
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                SessionHandler = new DefaultSupabaseSessionHandler()
            };

            return new Client(supabaseUrl, supabaseKey, options);
        });

        builder.Services.AddSingleton<IAuthSessionStore, MauiAuthSessionStore>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<SupabaseService>();
        builder.Services.AddSingleton<IObservationService>(sp => sp.GetRequiredService<SupabaseService>());
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<HistorialViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<EfemeridesViewModel>();

        // ── Geolocalización ───────────────────────────────────────────────────
        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<IGeolocationService, MauiGeolocationService>();

        // ── Ubicaciones Favoritas ─────────────────────────────────────────────
        builder.Services.AddSingleton<IFavoriteLocationsService, MauiFavoriteLocationsService>();
        builder.Services.AddSingleton<IDsoCatalogProvider, DsoCatalogProvider>();
        builder.Services.AddSingleton<IFilterSvgIconProvider, FilterSvgIconProvider>();

        // ── Estado compartido de ubicación ────────────────────────────────────
        builder.Services.AddSingleton<ILocationStateService, LocationStateService>();

        // ── Calculadora de tiempos de eclipses ────────────────────────────────
        builder.Services.AddSingleton<IEclipseCalculatorService, EclipseCalculatorService>();
        builder.Services.AddSingleton<IHomeAstronomyService, HomeAstronomyService>();
        builder.Services.AddSingleton<IEfemeridesAstronomyService, EfemeridesAstronomyService>();
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
			var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
			var http = httpFactory.CreateClient("default");
			var rss  = (RssFeedService)sp.GetRequiredService<IRssFeedService>();

			var wpProvider = new WpRestArticleProvider(http,
				baseUrl: "https://tubkala.com",
				sourceName: "Tubkala",
				languageCode: "es");

			var rssProviders = RssCatalog.Sources
				.Select(entry => new RssFeedArticleProvider(rss, entry.Source, languageCode: entry.LanguageCode))
				.ToArray();

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
