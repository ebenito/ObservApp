// ObservApp.Web.Client/Services/WebRssFeedService.cs
using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Implementación WASM de IRssFeedService.
/// Redirige las peticiones a través del proxy SSR para evitar CORS.
/// La lógica de parseo la hereda de RssFeedService (en Shared).
/// </summary>
public sealed class WebRssFeedService : RssFeedService
{
    public WebRssFeedService(HttpClient http, IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
        : base(http, localizer) { }

    protected override string ResolveUrl(string feedUrl)
        => $"/api/rss-proxy?url={Uri.EscapeDataString(feedUrl)}";
}