using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Proveedor de artículos que consume la API REST de WordPress (WP JSON API v2).
/// Ofrece paginación nativa mediante las cabeceras X-WP-Total y X-WP-TotalPages.
/// Extrae la imagen destacada desde yoast_head_json.schema.@graph (compatible con Yoast SEO).
/// 
/// En Blazor WASM el navegador bloquea cabeceras como User-Agent ("forbidden headers"),
/// por lo que el WAF de algunos hostings WordPress puede devolver HTML en lugar de JSON.
/// Para evitarlo, acepta un <c>proxyBaseUrl</c> opcional: cuando se configura,
/// las peticiones WP se enrutan por el proxy SSR (que sí puede añadir cabeceras reales).
/// </summary>
public sealed class WpRestArticleProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _sourceName;
    private readonly string _languageCode;

    // Si está configurado, las peticiones WP pasan por este proxy SSR
    // en lugar de ir directamente a _baseUrl (necesario en WASM).
    private readonly string? _proxyBaseUrl;

    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    /// <param name="http">HttpClient configurado.</param>
    /// <param name="baseUrl">URL base de la web WordPress, p.ej. "https://tubkala.com".</param>
    /// <param name="sourceName">Nombre de la fuente para mostrar en la UI.</param>
    /// <param name="languageCode">Código ISO del idioma principal del blog.</param>
    /// <param name="proxyBaseUrl">
    /// URL base del proxy SSR (p.ej. "https://localhost:7294").
    /// Cuando se especifica, las peticiones a la WP API se enrutan como:
    ///   GET {proxyBaseUrl}/api/wp-proxy?url={encoded_wp_api_url}
    /// Usar en Blazor WASM para evitar bloqueos WAF por User-Agent ausente.
    /// </param>
    public WpRestArticleProvider(
        HttpClient http,
        string baseUrl,
        string sourceName,
        string languageCode = "es",
        string? proxyBaseUrl = null)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _sourceName = sourceName;
        _languageCode = languageCode;
        _proxyBaseUrl = proxyBaseUrl?.TrimEnd('/');
    }

    public string? LastError { get; private set; }
    public bool LastCallUsedRssFallback { get; private set; }

    public async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        LastCallUsedRssFallback = false;

        try
        {
            var fields = "_fields=id,title,excerpt,content,date,link,yoast_head_json";
            var wpApiUrl = $"{_baseUrl}/wp-json/wp/v2/posts?page={page}&per_page={pageSize}&{fields}";

            // Si hay proxy configurado, enrutar por él (necesario en WASM)
            if (_proxyBaseUrl is not null)
                return await GetPageViaProxyAsync(wpApiUrl, page, pageSize, cancellationToken);

            // Llamada directa (MAUI / SSR donde el HttpClient puede añadir User-Agent)
            using var request = new HttpRequestMessage(HttpMethod.Get, wpApiUrl);
            TryAddBrowserHeaders(request);

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LastError = $"WP API respondió {(int)response.StatusCode}: {response.ReasonPhrase}";
                return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
            }

            return await ParseWpResponseAsync(response, page, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
        }
    }

    // ── Ruta por proxy SSR ────────────────────────────────────────────────────

    /// <summary>
    /// Enruta la petición WP API por el proxy SSR (/api/wp-proxy).
    /// El proxy añade User-Agent real y devuelve un wrapper JSON con los datos
    /// de paginación incrustados (porque WASM no puede leer cabeceras custom).
    /// </summary>
    private async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageViaProxyAsync(
        string wpApiUrl,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var proxyUrl = $"{_proxyBaseUrl}/api/wp-proxy?url={Uri.EscapeDataString(wpApiUrl)}";
            var json = await _http.GetStringAsync(proxyUrl, cancellationToken);

            // Verificar si la respuesta es HTML (proxy también bloqueado o no disponible)
            if (json.TrimStart().StartsWith('<'))
            {
                LastError = "El proxy WP devolvió HTML. Usando RSS como respaldo.";
                return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
            }

            if (!TryParseProxyResponse(json, out var posts, out var total, out var totalPages, out var parseError))
            {
                LastError = parseError;
                return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
            }

            if (posts.Count == 0)
                return (new(), false, total);

            var articles = posts.Select(ToArticle).ToList();
            bool hasNext = page < totalPages;

            return (articles, hasNext, total);
        }
        catch (Exception ex)
        {
            LastError = $"Proxy WP: {ex.Message}";
            return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
        }
    }

    // ── Ruta directa ─────────────────────────────────────────────────────────

    private async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> ParseWpResponseAsync(
        HttpResponseMessage response,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        int totalPages = 1;
        int totalCount = 0;

        if (response.Headers.TryGetValues("X-WP-TotalPages", out var tpValues))
            int.TryParse(tpValues.FirstOrDefault(), out totalPages);

        if (response.Headers.TryGetValues("X-WP-Total", out var tcValues))
            int.TryParse(tcValues.FirstOrDefault(), out totalCount);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Detectar HTML en lugar de JSON (WAF bloqueó aunque respondió 200)
        if (json.TrimStart().StartsWith('<'))
        {
            LastError = "WP API devolvió HTML. Usando RSS como respaldo.";
            return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
        }

        if (!TryParsePostsPayload(json, out var posts, out var parseError))
        {
            LastError = parseError;
            return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
        }

        if (posts.Count == 0)
            return (new(), false, totalCount);

        var articles = posts.Select(ToArticle).ToList();
        return (articles, page < totalPages, totalCount);
    }

    private static bool TryParseProxyResponse(
        string json,
        out List<WpPost> posts,
        out int total,
        out int totalPages,
        out string? error)
    {
        posts = new();
        total = 0;
        totalPages = 1;
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Proxy WP: respuesta JSON con formato no válido.";
                return false;
            }

            var root = doc.RootElement;

            if (root.TryGetProperty("total", out var totalElement) && totalElement.TryGetInt32(out var totalValue))
                total = totalValue;

            if (root.TryGetProperty("totalPages", out var totalPagesElement) && totalPagesElement.TryGetInt32(out var totalPagesValue))
                totalPages = totalPagesValue;

            if (!root.TryGetProperty("posts", out var postsElement))
            {
                error = "Proxy WP: respuesta sin el campo 'posts'.";
                return false;
            }

            return TryParsePostsElement(postsElement, out posts, out error);
        }
        catch (JsonException)
        {
            error = "Proxy WP: respuesta JSON inválida.";
            return false;
        }
    }

    private static bool TryParsePostsPayload(
        string json,
        out List<WpPost> posts,
        out string? error)
    {
        posts = new();
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                posts = JsonSerializer.Deserialize<List<WpPost>>(json, JsonOptions) ?? new();
                return true;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("posts", out var postsElement))
                    return TryParsePostsElement(postsElement, out posts, out error);

                if (TryGetWpApiMessage(root, out var apiMessage))
                {
                    error = $"WP API: {apiMessage}";
                    return false;
                }
            }

            error = "WP API devolvió un formato JSON no compatible.";
            return false;
        }
        catch (JsonException)
        {
            error = "WP API devolvió JSON inválido.";
            return false;
        }
    }

    private static bool TryParsePostsElement(
        JsonElement postsElement,
        out List<WpPost> posts,
        out string? error)
    {
        posts = new();
        error = null;

        if (postsElement.ValueKind == JsonValueKind.Array)
        {
            posts = JsonSerializer.Deserialize<List<WpPost>>(postsElement.GetRawText(), JsonOptions) ?? new();
            return true;
        }

        if (postsElement.ValueKind == JsonValueKind.Null)
            return true;

        if (postsElement.ValueKind == JsonValueKind.String)
        {
            var text = postsElement.GetString() ?? string.Empty;
            error = text.TrimStart().StartsWith('<')
                ? "WP API devolvió HTML. Usando RSS como respaldo."
                : "WP API devolvió texto en 'posts' en lugar de una lista.";
            return false;
        }

        if (postsElement.ValueKind == JsonValueKind.Object && TryGetWpApiMessage(postsElement, out var apiMessage))
        {
            error = $"WP API: {apiMessage}";
            return false;
        }

        error = "WP API devolvió un objeto 'posts' con formato no compatible.";
        return false;
    }

    private static bool TryGetWpApiMessage(JsonElement element, out string message)
    {
        message = string.Empty;

        if (!element.TryGetProperty("message", out var messageElement) || messageElement.ValueKind != JsonValueKind.String)
            return false;

        message = messageElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(message);
    }

    private static void TryAddBrowserHeaders(HttpRequestMessage request)
    {
        try
        {
            request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
            request.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        }
        catch { /* WASM rechaza User-Agent como forbidden header — se ignora */ }
    }

    // ── Fallback RSS ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fallback al feed RSS cuando la API REST no está disponible.
    /// LIMITACIÓN CONOCIDA: el feed RSS de WordPress solo contiene los últimos
    /// N posts (configurable en Ajustes > Lectura, por defecto 10), por lo que
    /// la paginación más allá de esos N posts no es posible vía RSS.
    /// </summary>
    private async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageFromRssFallbackAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            // Si hay proxy, enrutar el feed RSS también por él
            var feedUrl = $"{_baseUrl}/feed/";
            string xml;

            if (_proxyBaseUrl is not null)
            {
                var proxyUrl = $"{_proxyBaseUrl}/api/rss-proxy?url={Uri.EscapeDataString(feedUrl)}";
                xml = await _http.GetStringAsync(proxyUrl, cancellationToken);
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
                TryAddBrowserHeaders(request);
                using var response = await _http.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"{LastError} | RSS también falló: {(int)response.StatusCode}";
                    return (new(), false, 0);
                }
                xml = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            // Detectar HTML en respuesta RSS
            if (xml.TrimStart().StartsWith('<') &&
                xml.TrimStart().StartsWith("<!") || xml.TrimStart().StartsWith("<html"))
            {
                LastError = $"{LastError} | El feed RSS devolvió HTML.";
                return (new(), false, 0);
            }

            var allItems = ParseRssToArticles(xml);
            if (allItems.Count == 0)
            {
                LastError = $"{LastError} | El feed RSS no devolvió artículos.";
                return (new(), false, 0);
            }

            var pageItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            bool hasNext = allItems.Count > page * pageSize;

            LastError = null;
            LastCallUsedRssFallback = true;

            return (pageItems, hasNext, allItems.Count);
        }
        catch (Exception ex)
        {
            LastError = $"{LastError} | RSS fallback: {ex.Message}";
            return (new(), false, 0);
        }
    }

    private List<ArticleModel> ParseRssToArticles(string xml)
    {
        var items = new List<ArticleModel>();
        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch { return items; }

        var channel = doc.Root?.Element("channel") ?? doc.Root;
        if (channel is null) return items;

        XNamespace contentNs = "http://purl.org/rss/1.0/modules/content/";
        XNamespace mediaNs = "http://search.yahoo.com/mrss/";

        foreach (var item in channel.Elements("item"))
        {
            var title = DecodeHtml(item.Element("title")?.Value ?? string.Empty);
            var link = item.Element("link")?.Value?.Trim() ?? string.Empty;
            var guid = item.Element("guid")?.Value?.Trim();
            var contentEnc = item.Element(contentNs + "encoded")?.Value;
            var description = item.Element("description")?.Value;
            var contentHtml = !string.IsNullOrWhiteSpace(contentEnc) ? contentEnc! : description ?? string.Empty;
            var summary = StripHtml(description ?? contentHtml);
            var pubDate = ParseRssDate(item.Element("pubDate")?.Value);
            var imageUrl = ExtractRssImage(item, contentHtml, mediaNs);
            var id = !string.IsNullOrWhiteSpace(guid) ? guid! :
                              !string.IsNullOrWhiteSpace(link) ? link :
                              Guid.NewGuid().ToString();

            items.Add(new ArticleModel
            {
                Id = id,
                Title = title,
                Summary = summary,
                FullContent = contentHtml,
                SourceUrl = link,
                PublishedDate = pubDate,
                ImageUrl = imageUrl,
                SourceDisplayName = _sourceName,
                LanguageCode = _languageCode
            });
        }
        return items;
    }

    private static string? ExtractRssImage(XElement item, string contentHtml, XNamespace mediaNs)
    {
        var enclosure = item.Elements("enclosure")
            .FirstOrDefault(e => ((string?)e.Attribute("type"))?.StartsWith("image/") == true);
        if (enclosure is not null) return (string?)enclosure.Attribute("url");

        var media = item.Elements(mediaNs + "content")
            .FirstOrDefault(e => (string?)e.Attribute("medium") == "image"
                               || ((string?)e.Attribute("type"))?.StartsWith("image/") == true);
        if (media is not null) return (string?)media.Attribute("url");

        var match = Regex.Match(contentHtml, "<img[^>]+src=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DateTime ParseRssDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            return dto.UtcDateTime;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    private ArticleModel ToArticle(WpPost post) => new()
    {
        Id = post.Id.ToString(CultureInfo.InvariantCulture),
        Title = DecodeHtml(post.Title?.Rendered ?? string.Empty),
        Summary = StripHtml(post.Excerpt?.Rendered ?? string.Empty),
        FullContent = post.Content?.Rendered ?? string.Empty,
        SourceUrl = post.Link ?? string.Empty,
        PublishedDate = post.Date,
        ImageUrl = ExtractYoastImage(post.YoastHeadJson),
        SourceDisplayName = _sourceName,
        LanguageCode = _languageCode
    };

    private static string? ExtractYoastImage(YoastHeadJson? yoast)
    {
        if (yoast?.Schema?.Graph is null) return null;
        foreach (var node in yoast.Schema.Graph)
            if (node.Type is not null &&
                node.Type.Contains("ImageObject", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(node.Url))
                return node.Url;
        return null;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(noTags);
        var clean = Regex.Replace(decoded, @"\s+", " ").Trim();
        const int max = 240;
        if (clean.Length <= max) return clean;
        var cut = clean[..max];
        var sp = cut.LastIndexOf(' ');
        return (sp > 0 ? cut[..sp] : cut).TrimEnd() + "…";
    }

    private static string DecodeHtml(string text)
        => System.Net.WebUtility.HtmlDecode(text).Trim();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WpPost
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("link")] public string? Link { get; set; }
        [JsonPropertyName("title")] public WpRendered? Title { get; set; }
        [JsonPropertyName("excerpt")] public WpRendered? Excerpt { get; set; }
        [JsonPropertyName("content")] public WpRendered? Content { get; set; }
        [JsonPropertyName("yoast_head_json")] public YoastHeadJson? YoastHeadJson { get; set; }
    }

    private sealed class WpRendered
    {
        [JsonPropertyName("rendered")] public string? Rendered { get; set; }
    }

    private sealed class YoastHeadJson
    {
        [JsonPropertyName("schema")] public YoastSchema? Schema { get; set; }
    }

    private sealed class YoastSchema
    {
        [JsonPropertyName("@graph")] public List<YoastGraphNode>? Graph { get; set; }
    }

    private sealed class YoastGraphNode
    {
        [JsonPropertyName("@type")] public string? Type { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("contentUrl")] public string? ContentUrl { get; set; }
    }
}