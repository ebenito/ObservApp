using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Blazor;

namespace ObservApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// ── Licencia Syncfusion Community ────────────────────────────────────
		// TODO: mover a variable de entorno o Secret antes de publicar
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
			Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") ?? "License Key AQUI");

		var builder = MauiApp.CreateBuilder();

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
		// builder.Services.AddPlatformServices();

		return builder.Build();
	}
}