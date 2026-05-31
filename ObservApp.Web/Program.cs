using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Web.Client.Services;
using ObservApp.Web.Components;
using ObservApp.Web.Services;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

var syncfusionKey =
    builder.Configuration["SYNCFUSION_LICENSE_KEY"] ??
    builder.Configuration["SyncfusionLicenseKey"] ??
    string.Empty;

if (!string.IsNullOrEmpty(syncfusionKey))
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddLocalization();
builder.Services.AddScoped<ILocalizationService, SsrLocalizationService>();
builder.Services.AddScoped<ISettingsService, SsrSettingsService>();
builder.Services.AddScoped<IAppLifecycleService, WebAppLifecycleService>();
builder.Services.AddScoped<IGeolocationService, SsrGeolocationService>();
builder.Services.AddSingleton<AppState>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ObservApp.Shared.AssemblyReference).Assembly,
        typeof(ObservApp.Web.Client._Imports).Assembly);

app.Run();

