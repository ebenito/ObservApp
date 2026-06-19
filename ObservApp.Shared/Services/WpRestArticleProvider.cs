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
/// </summary>
public sealed class WpRestArticleProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _sourceName;
    private readonly string _languageCode;

    // User-Agent de navegador real. Algunos hostings WordPress (LiteSpeed + plugins
    // de seguridad / WAF tipo Wordfence) responden 403 a peticiones a /wp-json/*
    // que no parecen venir de un navegador (User-Agent ausente/genérico, Origin
    // distinto al del propio sitio, etc.), aunque la misma URL funcione perfectamente
    // al abrirla directamente. Por eso la añadimos en cada petición.
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    /// <param name="http">HttpClient configurado.</param>
    /// <param name="baseUrl">URL base de la web WordPress, p.ej. "https://tubkala.com".</param>
    /// <param name="sourceName">Nombre de la fuente para mostrar en la UI.</param>
    /// <param name="languageCode">Código ISO del idioma principal del blog.</param>
    public WpRestArticleProvider(
        HttpClient http,
        string baseUrl,
        string sourceName,
        string languageCode = "es")
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _sourceName = sourceName;
        _languageCode = languageCode;
    }

    public string? LastError { get; private set; }

    /// <summary>
    /// Indica si la última llamada exitosa se sirvió desde el feed RSS de respaldo
    /// en lugar de la API REST de WordPress (útil para diagnósticos / UI).
    /// </summary>
    public bool LastCallUsedRssFallback { get; private set; }

    /// <summary>
    /// Obtiene una página de posts de la API REST de WordPress.
    /// Si la API REST falla (403, 5xx, excepción de red…), recurre automáticamente
    /// al feed RSS del sitio (/feed/) para no interrumpir la experiencia del usuario.
    /// </summary>
    public async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        LastError = null;
        LastCallUsedRssFallback = false;

        try
        {
            // yoast_head_json incluye schema con imagen. Pedimos solo los campos necesarios.
            var fields = "_fields=id,title,excerpt,content,date,link,yoast_head_json";
            var url = $"{_baseUrl}/wp-json/wp/v2/posts?page={page}&per_page={pageSize}&{fields}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            TryAddBrowserHeaders(request);

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LastError = $"WP API respondió {(int)response.StatusCode}: {response.ReasonPhrase}";
                return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
            }

            // Paginación desde cabeceras WP
            int totalPages = 1;
            int totalCount = 0;

            if (response.Headers.TryGetValues("X-WP-TotalPages", out var tpValues))
                int.TryParse(tpValues.FirstOrDefault(), out totalPages);

            if (response.Headers.TryGetValues("X-WP-Total", out var tcValues))
                int.TryParse(tcValues.FirstOrDefault(), out totalCount);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var posts = JsonSerializer.Deserialize<List<WpPost>>(json, JsonOptions);

            if (posts is null || posts.Count == 0)
                return (new(), false, totalCount);

            var articles = posts.Select(ToArticle).ToList();
            return (articles, page < totalPages, totalCount);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return await GetPageFromRssFallbackAsync(page, pageSize, cancellationToken);
        }
    }

    /// <summary>
    /// Añade cabeceras "de navegador" a la petición. En Blazor WASM el navegador
    /// ignora/sustituye algunas (User-Agent es "forbidden header" en fetch); en
    /// MAUI y en el servidor (SSR) sí se envían y suelen evitar el bloqueo del WAF.
    /// </summary>
    private static void TryAddBrowserHeaders(HttpRequestMessage request)
    {
        try
        {
            request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
            request.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        }
        catch
        {
            // Si el runtime (p.ej. WASM) rechaza alguna cabecera, continuamos sin ella.
        }
    }

    // ── Fallback RSS ─────────────────────────────────────────────────────────

    /// <summary>
    /// Recurre al feed RSS estándar de WordPress cuando la API REST no está disponible.
    /// Si consigue artículos, limpia <see cref="LastError"/> para que la UI no muestre
    /// ningún error: simplemente se sirve el contenido por otra vía.
    /// </summary>
    private async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageFromRssFallbackAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var feedUrl = $"{_baseUrl}/feed/";

            using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
            TryAddBrowserHeaders(request);

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LastError = $"{LastError} | RSS también falló: {(int)response.StatusCode} {response.ReasonPhrase}";
                return (new(), false, 0);
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var allItems = ParseRssToArticles(xml);

            if (allItems.Count == 0)
            {
                LastError = $"{LastError} | El feed RSS no devolvió artículos.";
                return (new(), false, 0);
            }

            // El feed RSS de WordPress no tiene paginación real (solo trae los
            // últimos N posts configurados en Ajustes > Lectura, normalmente 10).
            // Paginamos en memoria sobre lo que el feed nos haya devuelto.
            var pageItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            bool hasNext = allItems.Count > page * pageSize;

            // Conseguimos servir contenido por RSS — no es un error para el usuario.
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
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch
        {
            return items;
        }

        var channel = doc.Root?.Element("channel") ?? doc.Root;
        if (channel is null) return items;

        XNamespace contentNs = "http://purl.org/rss/1.0/modules/content/";
        XNamespace mediaNs = "http://search.yahoo.com/mrss/";

        foreach (var item in channel.Elements("item"))
        {
            var title = DecodeHtml(item.Element("title")?.Value ?? string.Empty);
            var link = item.Element("link")?.Value?.Trim() ?? string.Empty;
            var guid = item.Element("guid")?.Value?.Trim();

            var contentEncoded = item.Element(contentNs + "encoded")?.Value;
            var description = item.Element("description")?.Value;
            var contentHtml = !string.IsNullOrWhiteSpace(contentEncoded)
                ? contentEncoded!
                : description ?? string.Empty;

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
        if (enclosure is not null)
            return (string?)enclosure.Attribute("url");

        var mediaContent = item.Elements(mediaNs + "content")
            .FirstOrDefault(e => (string?)e.Attribute("medium") == "image"
                                  || ((string?)e.Attribute("type"))?.StartsWith("image/") == true);
        if (mediaContent is not null)
            return (string?)mediaContent.Attribute("url");

        var match = Regex.Match(contentHtml, "<img[^>]+src=[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase);
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

    /// <summary>
    /// Extrae la imagen destacada de yoast_head_json.schema.@graph.
    /// Busca el nodo de tipo "ImageObject" y devuelve su "url".
    /// Estructura confirmada para Tubkala (Yoast SEO sin Jetpack):
    ///   yoast_head_json → schema → @graph → [ { @type: "ImageObject", url: "...", contentUrl: "..." } ]
    /// </summary>
    private static string? ExtractYoastImage(YoastHeadJson? yoast)
    {
        if (yoast?.Schema?.Graph is null) return null;

        foreach (var node in yoast.Schema.Graph)
        {
            if (node.Type is not null &&
                node.Type.Contains("ImageObject", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(node.Url))
            {
                return node.Url;
            }
        }

        return null;
    }

    // ── Helpers de texto ─────────────────────────────────────────────────────

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

    // ── Opciones JSON ─────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── DTOs de la API WP ─────────────────────────────────────────────────────

    private sealed class WpPost
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("title")]
        public WpRendered? Title { get; set; }

        [JsonPropertyName("excerpt")]
        public WpRendered? Excerpt { get; set; }

        [JsonPropertyName("content")]
        public WpRendered? Content { get; set; }

        [JsonPropertyName("yoast_head_json")]
        public YoastHeadJson? YoastHeadJson { get; set; }
    }

    private sealed class WpRendered
    {
        [JsonPropertyName("rendered")]
        public string? Rendered { get; set; }
    }

    private sealed class YoastHeadJson
    {
        [JsonPropertyName("schema")]
        public YoastSchema? Schema { get; set; }
    }

    private sealed class YoastSchema
    {
        [JsonPropertyName("@graph")]
        public List<YoastGraphNode>? Graph { get; set; }
    }

    private sealed class YoastGraphNode
    {
        [JsonPropertyName("@type")]
        public string? Type { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("contentUrl")]
        public string? ContentUrl { get; set; }
    }
}