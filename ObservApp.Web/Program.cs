using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Shared.ViewModels;
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
builder.Services.AddScoped<INavigationHistoryService, NavigationHistoryService>();
builder.Services.AddScoped<IAppLifecycleService, WebAppLifecycleService>();
builder.Services.AddScoped<IGeolocationService, SsrGeolocationService>();
builder.Services.AddScoped<IFavoriteLocationsService, SsrFavoriteLocationsService>();
builder.Services.AddScoped<IDsoCatalogProvider, DsoCatalogProvider>();
builder.Services.AddScoped<ILocationStateService, LocationStateService>();
builder.Services.AddScoped<IExternalLinkService, SsrExternalLinkService>();
builder.Services.AddScoped<ITextToSpeechService, SsrTextToSpeechService>();
builder.Services.AddScoped<IEclipseCalculatorService, EclipseCalculatorService>();
builder.Services.AddScoped<IHomeAstronomyService, HomeAstronomyService>();
builder.Services.AddScoped<IEfemeridesAstronomyService, EfemeridesAstronomyService>();
builder.Services.AddScoped<IEclipseAudioService, SsrEclipseAudioService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddScoped<IAuthService, SsrAuthService>();
builder.Services.AddScoped<IObservationService, SsrObservationService>();
builder.Services.AddTransient<AuthViewModel>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<EfemeridesViewModel>();
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
        "astrobitacora.com",
        "astrobitos.org",
        "megaphone.fm",
        "esa.int",
        // Inglés
        "nasa.gov",
        "eso.org",
        "skyandtelescope.org",
        "astronomy.com",
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

// ── Proxy para API REST de WordPress ─────────────────────────────────────────
app.MapGet("/api/wp-proxy", async (string url, HttpClient http) =>
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        !uri.Host.EndsWith("tubkala.com", StringComparison.OrdinalIgnoreCase) ||
        !uri.PathAndQuery.Contains("/wp-json/"))
        return Results.BadRequest("URL no permitida");

    try
    {
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

        using var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return Results.Problem($"WP API respondió {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        if (json.TrimStart().StartsWith('<'))
            return Results.Problem("WP API devolvió HTML en lugar de JSON");

        var totalPages = response.Headers.TryGetValues("X-WP-TotalPages", out var tp)
            ? tp.FirstOrDefault() ?? "1" : "1";
        var totalCount = response.Headers.TryGetValues("X-WP-Total", out var tc)
            ? tc.FirstOrDefault() ?? "0" : "0";

        var wrapper = $"{{\"total\":{totalCount},\"totalPages\":{totalPages},\"posts\":{json}}}";
        return Results.Content(wrapper, "application/json; charset=utf-8");
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
