using ObservApp.Shared.Services;
using ObservApp.Shared.State;
using ObservApp.Shared.ViewModels;
using ObservApp.Web.Client.Services;
using ObservApp.Web.Components;
using ObservApp.Web.Services;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);
ConfigureBuilder(builder);

var app = builder.Build();
ConfigureApplication(app);

await app.RunAsync();

static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    RegisterSyncfusionLicense(builder.Configuration);

    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();

    builder.Services.AddSyncfusionBlazor();
    builder.Services.AddLocalization();
    builder.Services.AddSingleton<IVersionService, VersionService>();
    builder.Services.AddScoped<ILocalizationService, SsrLocalizationService>();
    builder.Services.AddScoped<ISettingsService, SsrSettingsService>();
    builder.Services.AddScoped<INavigationHistoryService, NavigationHistoryService>();
    builder.Services.AddScoped<IAppLifecycleService, WebAppLifecycleService>();
    builder.Services.AddScoped<IGeolocationService, SsrGeolocationService>();
    builder.Services.AddScoped<IFavoriteLocationsService, SsrFavoriteLocationsService>();
    builder.Services.AddScoped<IDsoCatalogProvider, DsoCatalogProvider>();
    builder.Services.AddSingleton<IFilterSvgIconProvider, FilterSvgIconProvider>();
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

    builder.Services.AddScoped<IArticleService>(sp =>
        new ArticleService(
            wpProvider: null,
            rssProviders: Array.Empty<RssFeedArticleProvider>()));

    builder.Services.AddHttpClient();
    AddCorsPolicy(builder);
}

static void RegisterSyncfusionLicense(IConfiguration configuration)
{
    var syncfusionKey =
        configuration["SYNCFUSION_LICENSE_KEY"] ??
        configuration["SyncfusionLicenseKey"] ??
        string.Empty;

    if (!string.IsNullOrEmpty(syncfusionKey))
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);
}

static void AddCorsPolicy(WebApplicationBuilder builder)
{
    var allowedOrigins = GetAllowedOrigins(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("RssProxy", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader());
    });
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    return configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
        configuration["Cors:AllowedOrigins"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? ["https://localhost:5001", "https://localhost:7184", "http://localhost:5000", "http://localhost:7294"];
}

static void ConfigureApplication(WebApplication app)
{
    app.UseCors("RssProxy");
    MapClientConfigEndpoint(app);
    MapRssProxyEndpoint(app);
    MapWpProxyEndpoint(app);

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

    app.Use(async (context, next) =>
    {
        await next();
        if (context.Response.StatusCode == 404)
        {
            context.Response.Redirect("/not-found", permanent: false);
        }
    });
}

static void MapClientConfigEndpoint(WebApplication app)
{
    app.MapGet("/api/client-config", (IConfiguration configuration) => Results.Json(new
    {
        SupabaseUrl = (configuration["SupabaseUrl"] ?? string.Empty).Trim(),
        SupabaseAnonKey = (configuration["SupabaseAnonKey"] ?? string.Empty).Trim()
    }));
}

static void MapRssProxyEndpoint(WebApplication app)
{
    app.MapGet("/api/rss-proxy", async (string url, HttpClient http) =>
    {
        var allowed = new[]
        {
            "tubkala.com",
            "astrobitacora.com",
            "astrobitos.org",
            "megaphone.fm",
            "esa.int",
            "nasa.gov",
            "eso.org",
            "skyandtelescope.org",
            "astronomy.com",
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
}

static void MapWpProxyEndpoint(WebApplication app)
{
    app.MapGet("/api/wp-proxy", async (string url, HttpClient http) =>
    {
        if (!TryGetAllowedWpUrl(url, out var uri))
            return Results.BadRequest("URL no permitida");

        try
        {
            ConfigureWpRequestHeaders(http);

            using var response = await http.GetAsync(uri!);
            if (!response.IsSuccessStatusCode)
                return Results.Problem($"WP API respondió {(int)response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            if (IsHtmlWpResponse(json))
                return Results.Problem("WP API devolvió HTML en lugar de JSON");

            var wrapper = BuildWpProxyPayload(response, json);
            return Results.Content(wrapper, "application/json; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });
}

static bool TryGetAllowedWpUrl(string url, out Uri? uri)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
        !parsed.Host.EndsWith("tubkala.com", StringComparison.OrdinalIgnoreCase) ||
        !parsed.PathAndQuery.Contains("/wp-json/", StringComparison.OrdinalIgnoreCase))
    {
        uri = null;
        return false;
    }

    uri = parsed;
    return true;
}

static void ConfigureWpRequestHeaders(HttpClient http)
{
    http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
}

static bool IsHtmlWpResponse(string json)
{
    return json.TrimStart().StartsWith('<');
}

static string BuildWpProxyPayload(HttpResponseMessage response, string json)
{
    var totalPages = response.Headers.TryGetValues("X-WP-TotalPages", out var tp)
        ? tp.FirstOrDefault() ?? "1" : "1";
    var totalCount = response.Headers.TryGetValues("X-WP-Total", out var tc)
        ? tc.FirstOrDefault() ?? "0" : "0";

    return $"{{\"total\":{totalCount},\"totalPages\":{totalPages},\"posts\":{json}}}";
}
