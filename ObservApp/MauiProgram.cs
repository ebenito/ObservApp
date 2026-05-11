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

		// ── Configuración — User Secrets (local) y variables de entorno ─────
#if DEBUG
		builder.Configuration.AddUserSecrets<App>();
#endif
		builder.Configuration.AddEnvironmentVariables();

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

		// ── HttpClient ───────────────────────────────────────────────────────
		builder.Services.AddHttpClient();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// TODO: registrar servicios a medida que se vayan creando:
		// builder.Services.AddSingleton<SupabaseService>();
		// builder.Services.AddSingleton<AuthService>();
		// builder.Services.AddSingleton<GeolocationService>();
		// builder.Services.AddSingleton<AstronomyService>();
		// builder.Services.AddSingleton<ThemeService>();
		// builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();

		return builder.Build();
	}
}