using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Shared.ViewModels;
using ObservApp.Web.Client.Services;
using Supabase;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

using var startupConfigHttp = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
// En modo HOSTED: intentar primero /api/client-config del servidor.
// En modo STANDALONE: saltar directamente a appsettings.Local.json.
#if !STANDALONE_WASM
await LoadOptionalConfigurationAsync("api/client-config");
#endif
await LoadOptionalConfigurationAsync("appsettings.Local.json");

async Task LoadOptionalConfigurationAsync(string relativeUrl)
{
	try
	{
		var json = await startupConfigHttp.GetStringAsync(relativeUrl);
		try { Console.WriteLine($"[ConfigLoad] {relativeUrl} → {json}"); } catch { }

		var bytes = System.Text.Encoding.UTF8.GetBytes(json);
		await using var ms = new System.IO.MemoryStream(bytes);
		builder.Configuration.AddJsonStream(ms);
	}
	catch (System.Net.Http.HttpRequestException hre)
	{
		try { Console.WriteLine($"[ConfigLoad][HTTP] {relativeUrl} no disponible: {hre.Message}"); } catch { }
	}
	catch (System.Text.Json.JsonException je)
	{
		try { Console.WriteLine($"[ConfigLoad][JSON] Error parseando {relativeUrl}: {je.Message}"); } catch { }
	}
	catch (Exception ex)
	{
		try { Console.WriteLine($"[ConfigLoad][ERR] al cargar {relativeUrl}: {ex.Message}"); } catch { }
	}
}

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
builder.Services.AddSingleton<INavigationHistoryService, NavigationHistoryService>();
builder.Services.AddSingleton<IAppLifecycleService, WebClientAppLifecycleService>();
builder.Services.AddSingleton<IGeolocationService, WebGeolocationService>();
builder.Services.AddSingleton<IFavoriteLocationsService, WebFavoriteLocationsService>();
builder.Services.AddSingleton<IDsoCatalogProvider, DsoCatalogProvider>();
builder.Services.AddSingleton<ILocationStateService, LocationStateService>();
builder.Services.AddSingleton<IExternalLinkService, WebExternalLinkService>();
builder.Services.AddSingleton<ITextToSpeechService, WebTextToSpeechService>();
builder.Services.AddSingleton<IEclipseCalculatorService, EclipseCalculatorService>();
builder.Services.AddSingleton<IHomeAstronomyService, HomeAstronomyService>();
builder.Services.AddSingleton<IEfemeridesAstronomyService, EfemeridesAstronomyService>();
builder.Services.AddSingleton<IEclipseAudioService, WebEclipseAudioService>();
builder.Services.AddSingleton<IVersionService, VersionService>();
builder.Services.AddSingleton<AppState>();

// ── Autenticación y persistencia con Supabase ────────────────────────────────
var supabaseUrl = (builder.Configuration["SupabaseUrl"] ?? string.Empty).Trim();
var supabaseKey = (builder.Configuration["SupabaseAnonKey"] ?? string.Empty).Trim();

try
{
	Console.WriteLine($"[Startup] SupabaseUrl='{supabaseUrl}'");
	Console.WriteLine($"[Startup] SupabaseAnonKey present={(string.IsNullOrEmpty(supabaseKey) ? "false" : "true")}");
}
catch { }

if (!string.IsNullOrEmpty(supabaseUrl) && !Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var _))
{
	Console.WriteLine($"[Startup][ERROR] SupabaseUrl no es una URL válida: '{supabaseUrl}'");
}

builder.Services.AddSingleton(sp =>
{
	var options = new SupabaseOptions
	{
		AutoRefreshToken = true,
		AutoConnectRealtime = true,
		SessionHandler = new DefaultSupabaseSessionHandler()
	};

	return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

builder.Services.AddSingleton<IAuthSessionStore, InMemoryAuthSessionStore>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSingleton<IObservationService>(
	sp => sp.GetRequiredService<SupabaseService>());
builder.Services.AddTransient<AuthViewModel>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<EfemeridesViewModel>();

// ── IRssFeedService vía proxy SSR (evita CORS en WASM) ──────────────────────
builder.Services.AddHttpClient<IRssFeedService, WebRssFeedService>(client =>
{
	client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// ── IArticleService: WP API vía proxy SSR + RSS vía proxy SSR ────────────────
// WpRestArticleProvider usa el proxy SSR (/api/wp-proxy) porque en WASM el
// navegador bloquea User-Agent como "forbidden header", lo que hace que el
// WAF de Tubkala devuelva HTML en lugar de JSON.
// RssFeedArticleProvider usa WebRssFeedService que ya enruta por /api/rss-proxy.
builder.Services.AddHttpClient("ProxyHttp", client =>
{
	client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddSingleton<IArticleService>(sp =>
{
	var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
	var proxyHttp = httpFactory.CreateClient("ProxyHttp");
	var rss = (RssFeedService)sp.GetRequiredService<IRssFeedService>();

	// proxyBaseUrl = BaseAddress del propio host WASM (que es el servidor SSR)
	// → las peticiones /api/wp-proxy las resuelve el servidor SSR
	var wpProvider = new WpRestArticleProvider(proxyHttp,
		baseUrl: "https://tubkala.com",
		sourceName: "Tubkala",
		languageCode: "es",
		proxyBaseUrl: builder.HostEnvironment.BaseAddress.TrimEnd('/'));

	// ── Fuentes RSS — Español ────────────────────────────────────────────────
	var astrobit = new RssSource("astrobit", "Astrobitácora", "https://www.astrobitacora.com/feed/", IsBuiltIn: true);
	var astrobites = new RssSource("astrobites", "Astrobites ES", "https://astrobitos.org/feed/", IsBuiltIn: true);
	var esaes = new RssSource("esaes", "ESA España", "https://www.esa.int/rssfeed/Spain", IsBuiltIn: true);
	var nasaes = new RssSource("nasaes", "Universo curioso de la NASA", "https://feeds.megaphone.fm/nationalaeronauticsandspaceadministration5412631684", IsBuiltIn: true);

	// ── Fuentes RSS — Inglés ─────────────────────────────────────────────────
	var nasa = new RssSource("nasa", "NASA", "https://www.nasa.gov/feed/", IsBuiltIn: true);
	var nasaimg = new RssSource("nasaimg", "NASA Image of the Day", "https://www.nasa.gov/feeds/iotd-feed/", IsBuiltIn: true);
	var esa = new RssSource("esa", "ESA", "http://www.esa.int/rssfeed/Our_Activities/Space_Science", IsBuiltIn: true);
	var eso = new RssSource("eso", "ESO", "https://www.eso.org/public/blog/feed/", IsBuiltIn: true);
	var skytel = new RssSource("skytel", "Sky & Telescope", "https://skyandtelescope.org/feed/", IsBuiltIn: true);
	var astromag = new RssSource("astromag", "Astronomy Magazine", "https://www.astronomy.com/feed/", IsBuiltIn: true);

	var rssProviders = new[]
	{
		new RssFeedArticleProvider(rss, astrobit,   languageCode: "es"),
		new RssFeedArticleProvider(rss, astrobites, languageCode: "es"),
		new RssFeedArticleProvider(rss, esaes,      languageCode: "es"),
		new RssFeedArticleProvider(rss, nasaes,     languageCode: "es"),
		new RssFeedArticleProvider(rss, nasa,       languageCode: "en"),
		new RssFeedArticleProvider(rss, nasaimg,    languageCode: "en"),
		new RssFeedArticleProvider(rss, esa,        languageCode: "en"),
		new RssFeedArticleProvider(rss, eso,        languageCode: "en"),
		new RssFeedArticleProvider(rss, skytel,     languageCode: "en"),
		new RssFeedArticleProvider(rss, astromag,   languageCode: "en"),
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
