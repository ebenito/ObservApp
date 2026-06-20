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
builder.Services.AddScoped<IFavoriteLocationsService, SsrFavoriteLocationsService>();
builder.Services.AddScoped<IExternalLinkService, SsrExternalLinkService>();
builder.Services.AddScoped<ITextToSpeechService, SsrTextToSpeechService>();
builder.Services.AddScoped<IEclipseCalculatorService, EclipseCalculatorService>();
builder.Services.AddScoped<IEclipseAudioService, SsrEclipseAudioService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddSingleton<AppState>();

// ── IArticleService — stub SSR (cero artículos en render servidor) ───────────
// La carga real ocurre tras hidratación en el cliente WASM.
builder.Services.AddScoped<IArticleService>(sp =>
    new ArticleService(
        wpProvider: null,
        rssProviders: Array.Empty<RssFeedArticleProvider>()));

builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("RssProxy", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("RssProxy");

app.MapGet("/api/rss-proxy", async (string url, HttpClient http) =>
{
    // Whitelist de dominios permitidos — incluye todas las fuentes de Señales
    var allowed = new[]
    {
        "tubkala.com",
        // Español
        "agenciasinc.es",
        "iac.es",
        "investigacionyciencia.es",
        "astropaf.com",
        // Inglés
        "jpl.nasa.gov",
        "esa.int",
        "eso.org",
        "skyandtelescope.org",
        "astronomy.com",
        "space.com",
        // Legacy (compatibilidad)
        "apod.nasa.gov",
        "spaceweather.com",
        "blogs.esa.int",
    };

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        !allowed.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest("Dominio no permitido");

    try
    {
        http.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent", "ObservApp/1.0 RSS Reader");
        var xml = await http.GetStringAsync(url);
        return Results.Content(xml, "application/xml; charset=utf-8");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ObservApp.Shared.AssemblyReference).Assembly,
        typeof(ObservApp.Web.Client._Imports).Assembly);

// Middleware para manejar 404s dinámicamente
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Response.Redirect("/not-found", permanent: false);
    }
});

app.Run();
