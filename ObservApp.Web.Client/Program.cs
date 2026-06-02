using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Web.Client.Services;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── Licencia Syncfusion — debe registrarse también en el cliente WASM ────────
//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("TU_CLAVE_AQUI");
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
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
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