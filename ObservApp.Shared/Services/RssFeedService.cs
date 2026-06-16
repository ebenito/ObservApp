using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ObservApp.Shared.Services;

/// <summary>
/// Implementación compartida de <see cref="IRssFeedService"/>.
/// Parsea RSS 2.0 (formato WordPress estándar, con content:encoded) y Atom.
/// No depende de System.ServiceModel.Syndication para mantener el peso bajo en WASM.
/// </summary>
public class RssFeedService : IRssFeedService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, string?> _lastErrors = new();

    // ── Namespaces XML habituales en feeds RSS/Atom ─────────────────────────
    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace MediaNs = "http://search.yahoo.com/mrss/";

    public RssFeedService(HttpClient http)
    {
        _http = http;
    }

    public string? GetLastError(string sourceId)
        => _lastErrors.TryGetValue(sourceId, out var err) ? err : null;

    public async Task<List<RssFeedItem>> GetItemsAsync(
        IEnumerable<RssSource> sources,
        CancellationToken cancellationToken = default)
    {
        var all = new List<RssFeedItem>();

        foreach (var source in sources)
        {
            var items = await GetItemsFromSourceAsync(source, cancellationToken);
            all.AddRange(items);
        }

        return all
            .OrderByDescending(i => i.PublishedUtc)
            .ToList();
    }

    public async Task<List<RssFeedItem>> GetItemsFromSourceAsync(RssSource source, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        const int initialDelayMs = 1000;
        const string htmlIndicator = "<!DOCTYPE";

        try
        {
            _lastErrors[source.Id] = null;

            string xml = string.Empty;
            int attempt = 0;
            string resolvedUrl = ResolveUrl(source.Url);

            // Reintentos con espera progresiva si se recibe HTML
            while (attempt < maxRetries)
            {
                xml = await _http.GetStringAsync(resolvedUrl, cancellationToken);

                // Verificar si es HTML (página de verificación de Inmunify 360 u otra)
                if (!xml.TrimStart().StartsWith(htmlIndicator, StringComparison.OrdinalIgnoreCase))
                {
                    break; // Es XML válido, salir del bucle de reintentos
                }

                attempt++;
                if (attempt < maxRetries)
                {
                    // Espera progresiva: 1s, 2s, 3s
                    int delayMs = initialDelayMs * attempt;
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            // Si después de reintentos aún es HTML, lanzar excepción con la URL
            if (xml.TrimStart().StartsWith(htmlIndicator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"OPEN_BROWSER:{resolvedUrl}|El servidor devolvió HTML en lugar de XML. " +
                    "La URL puede estar protegida por Inmunify 360 u otro sistema de verificación.");
            }

            var doc = XDocument.Parse(xml);

            var root = doc.Root;
            if (root is null)
                throw new InvalidOperationException("El documento XML no tiene elemento raíz.");

            // ── Atom: <feed><entry>... ───────────────────────────────────────
            if (root.Name == AtomNs + "feed")
                return ParseAtom(root, source);

            // ── RSS 2.0 / RSS 1.0: <rss><channel><item>... ──────────────────
            var channel = root.Element("channel") ?? root;
            return ParseRss(channel, source);
        }
        catch (Exception ex)
        {
            _lastErrors[source.Id] = ex.Message;
            return new List<RssFeedItem>();
        }
    }

    // ── RSS 2.0 ──────────────────────────────────────────────────────────────
    private static List<RssFeedItem> ParseRss(XElement channel, RssSource source)
    {
        var channelLang = NormalizeLanguageCode(channel.Element("language")?.Value);

        var items = new List<RssFeedItem>();

        foreach (var item in channel.Elements("item"))
        {
            var title = CleanText(item.Element("title")?.Value);
            var link = item.Element("link")?.Value?.Trim() ?? string.Empty;

            var contentEncoded = item.Element(ContentNs + "encoded")?.Value;
            var description = item.Element("description")?.Value;

            var contentHtml = !string.IsNullOrWhiteSpace(contentEncoded)
                ? contentEncoded!
                : description ?? string.Empty;

            var summary = BuildSummary(description, contentHtml);

            var pubDate = ParseDate(
                item.Element("pubDate")?.Value
                ?? item.Element(DcNs + "date")?.Value);

            var imageUrl = ExtractImage(item, contentHtml, MediaNs);

            items.Add(new RssFeedItem(
                Title: title,
                Summary: summary,
                ContentHtml: contentHtml,
                Link: link,
                PublishedUtc: pubDate,
                ImageUrl: imageUrl,
                SourceName: source.Name,
                LanguageCode: channelLang));
        }

        return items;
    }

    // ── Atom ─────────────────────────────────────────────────────────────────
    private static List<RssFeedItem> ParseAtom(XElement feed, RssSource source)
    {
        var feedLang = NormalizeLanguageCode(
            feed.Attribute(XNamespace.Xml + "lang")?.Value);

        var items = new List<RssFeedItem>();

        foreach (var entry in feed.Elements(AtomNs + "entry"))
        {
            var title = CleanText(entry.Element(AtomNs + "title")?.Value);

            var link = entry.Elements(AtomNs + "link")
                .FirstOrDefault(l => (string?)l.Attribute("rel") is null or "alternate")
                ?.Attribute("href")?.Value ?? string.Empty;

            var contentEl = entry.Element(AtomNs + "content");
            var summaryEl = entry.Element(AtomNs + "summary");

            var contentHtml = contentEl?.Value ?? summaryEl?.Value ?? string.Empty;
            var summary = BuildSummary(summaryEl?.Value, contentHtml);

            var pubDate = ParseDate(
                entry.Element(AtomNs + "published")?.Value
                ?? entry.Element(AtomNs + "updated")?.Value);

            var entryLang = NormalizeLanguageCode(
                entry.Attribute(XNamespace.Xml + "lang")?.Value) ?? feedLang;

            var imageUrl = ExtractImage(entry, contentHtml, MediaNs);

            items.Add(new RssFeedItem(
                Title: title,
                Summary: summary,
                ContentHtml: contentHtml,
                Link: link,
                PublishedUtc: pubDate,
                ImageUrl: imageUrl,
                SourceName: source.Name,
                LanguageCode: entryLang ?? "es"));
        }

        return items;
    }

    // ── Helpers comunes ──────────────────────────────────────────────────────

    /// <summary>
    /// Genera un resumen corto en texto plano a partir de la descripción
    /// o, si no existe, del contenido completo (truncado).
    /// </summary>
    private static string BuildSummary(string? description, string contentHtml)
    {
        var source = !string.IsNullOrWhiteSpace(description) ? description! : contentHtml;
        var plain = StripHtml(source);

        const int maxLen = 240;
        if (plain.Length <= maxLen) return plain;

        var cut = plain[..maxLen];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > 0) cut = cut[..lastSpace];
        return cut.TrimEnd() + "…";
    }

    /// <summary>
    /// Extrae la imagen destacada: primero busca &lt;enclosure&gt; o media:content,
    /// y si no existe, la primera &lt;img src="..."&gt; dentro del HTML del artículo
    /// (caso típico de WordPress, que no incluye enclosure por defecto).
    /// </summary>
    private static string? ExtractImage(XElement item, string contentHtml, XNamespace mediaNs)
    {
        // <enclosure url="..." type="image/...">
        var enclosure = item.Elements("enclosure")
            .FirstOrDefault(e => ((string?)e.Attribute("type"))?.StartsWith("image/") == true);
        if (enclosure is not null)
            return (string?)enclosure.Attribute("url");

        // <media:content url="..." medium="image">
        var mediaContent = item.Elements(mediaNs + "content")
            .FirstOrDefault(e => (string?)e.Attribute("medium") == "image"
                                  || ((string?)e.Attribute("type"))?.StartsWith("image/") == true);
        if (mediaContent is not null)
            return (string?)mediaContent.Attribute("url");

        // Fallback: primera <img> dentro del contenido HTML
        var match = Regex.Match(contentHtml, "<img[^>]+src=[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Elimina etiquetas HTML y normaliza espacios/entidades básicas.
    /// </summary>
    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(noTags);
        var collapsed = Regex.Replace(decoded, @"\s+", " ");
        return collapsed.Trim();
    }

    /// <summary>
    /// Limpia el título: decodifica entidades HTML y recorta espacios.
    /// </summary>
    private static string CleanText(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : System.Net.WebUtility.HtmlDecode(text).Trim();

    /// <summary>
    /// Parsea fechas en formato RFC 822 (RSS) o ISO 8601 (Atom/dc:date).
    /// Devuelve UTC; si no se puede parsear, devuelve DateTime.MinValue.
    /// </summary>
    private static DateTime ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            return dto.UtcDateTime;

        // Formato RFC 822 típico: "Mon, 09 Jun 2026 10:00:00 +0000"
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;

        return DateTime.MinValue;
    }

    /// <summary>
    /// Normaliza un código de idioma tipo "es-ES" a "es". Devuelve null si está vacío.
    /// </summary>
    private static string? NormalizeLanguageCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        var dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed[..dash].ToLowerInvariant() : trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// Permite que las subclases redirijan la URL (por ejemplo, a través de un proxy).
    /// Por defecto devuelve la URL original.
    /// </summary>
    protected virtual string ResolveUrl(string feedUrl) => feedUrl;

}