namespace ObservApp.Shared.Services;

public record RssFeedItem(
    string Title,
    string Summary,
    string ContentHtml,
    string Link,
    DateTime PublishedUtc,
    string? ImageUrl,
    string SourceName,
    string LanguageCode
);

public record RssSource(
    string Id,
    string Name,
    string Url,
    bool IsBuiltIn
);

public interface IRssFeedService
{
    /// <summary>
    /// Descarga y combina los artículos de varias fuentes, ordenados por fecha descendente.
    /// </summary>
    Task<List<RssFeedItem>> GetItemsAsync(IEnumerable<RssSource> sources, CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga los artículos de una sola fuente.
    /// </summary>
    Task<List<RssFeedItem>> GetItemsFromSourceAsync(RssSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Último error producido al consultar una fuente (por Id de fuente).
    /// </summary>
    string? GetLastError(string sourceId);
}