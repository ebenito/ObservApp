using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Proveedor de artículos que consume feeds RSS/Atom.
/// Reutiliza <see cref="RssFeedService"/> para el parsing y normaliza
/// los resultados a <see cref="ArticleModel"/>.
///
/// RSS no tiene paginación nativa: se descarga la lista completa una vez
/// y se pagina en memoria. El resultado se cachea durante la sesión
/// para evitar descargas repetidas al cambiar de página.
/// </summary>
public sealed class RssFeedArticleProvider
{
    private readonly RssFeedService _rss;
    private readonly RssSource _source;
    private readonly string _languageCode;

    // Cache en memoria para la sesión — evita descargar el feed en cada cambio de página
    private List<ArticleModel>? _cachedItems;

    /// <param name="rss">Servicio de parsing RSS/Atom.</param>
    /// <param name="source">Definición de la fuente (Id, Name, Url).</param>
    /// <param name="languageCode">
    /// Código ISO del idioma de la fuente ("es", "en"...).
    /// Se usa si el feed no declara &lt;language&gt; o si es Atom sin xml:lang.
    /// </param>
    public RssFeedArticleProvider(RssFeedService rss, RssSource source, string languageCode = "es")
    {
        _rss = rss;
        _source = source;
        _languageCode = languageCode;
    }

    public string? LastError { get; private set; }

    /// <summary>
    /// Obtiene una página de artículos paginando en memoria sobre el feed completo.
    /// Si los datos ya están en caché, no descarga de nuevo el feed.
    /// </summary>
    public async Task<(List<ArticleModel> Items, bool HasNextPage, int Total)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        if (_cachedItems is null)
        {
            var raw = await _rss.GetItemsFromSourceAsync(_source, cancellationToken);
            LastError = _rss.GetLastError(_source.Id);
            _cachedItems = raw.Select(ToArticle).ToList();
        }

        if (_cachedItems.Count == 0)
            return (new(), false, 0);

        var total   = _cachedItems.Count;
        var skip    = (page - 1) * pageSize;
        var items   = _cachedItems.Skip(skip).Take(pageSize).ToList();
        var hasNext = (skip + pageSize) < total;

        return (items, hasNext, total);
    }

    /// <summary>Invalida la caché, forzando una nueva descarga en la próxima llamada.</summary>
    public void Invalidate() => _cachedItems = null;

    private ArticleModel ToArticle(RssFeedItem item) => new()
    {
        Id                = item.Link,
        Title             = item.Title,
        Summary           = item.Summary,
        FullContent       = item.ContentHtml,
        SourceUrl         = item.Link,
        PublishedDate     = item.PublishedUtc,
        ImageUrl          = item.ImageUrl,
        SourceDisplayName = item.SourceName,
        // Usar el idioma declarado en el feed si existe; si no, el del constructor
        LanguageCode      = !string.IsNullOrWhiteSpace(item.LanguageCode)
                            ? item.LanguageCode
                            : _languageCode
    };
}
