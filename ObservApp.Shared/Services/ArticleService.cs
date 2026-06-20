using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Implementación de <see cref="IArticleService"/> que combina artículos
/// de múltiples proveedores (API REST WordPress + feeds RSS/Atom),
/// los normaliza a <see cref="ArticleModel"/> y los pagina de forma unificada.
///
/// Estrategia de paginación híbrida:
/// — WP API: paginación nativa en el servidor (X-WP-Total / X-WP-TotalPages).
/// — RSS: descarga completa en la primera llamada, paginación en memoria con caché.
/// — Todos los proveedores se consultan en paralelo con Task.WhenAll.
/// — Los resultados se combinan y ordenan por fecha descendente.
/// </summary>
public sealed class ArticleService : IArticleService
{
    private readonly WpRestArticleProvider? _wpProvider;
    private readonly List<RssFeedArticleProvider> _rssProviders;

    public string? LastError { get; private set; }

    /// <param name="wpProvider">Proveedor de la API REST de WordPress (null si no se configura).</param>
    /// <param name="rssProviders">Lista de proveedores RSS (puede estar vacía).</param>
    public ArticleService(
        WpRestArticleProvider? wpProvider,
        IEnumerable<RssFeedArticleProvider> rssProviders)
    {
        _wpProvider   = wpProvider;
        _rssProviders = rssProviders.ToList();
    }

    public async Task<ArticlePage> GetPageAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        // Lanzar todas las fuentes en paralelo
        var tasks = new List<Task<(List<ArticleModel> Items, bool HasNextPage, int Total)>>();

        if (_wpProvider is not null)
            tasks.Add(_wpProvider.GetPageAsync(page, pageSize, cancellationToken));

        foreach (var rss in _rssProviders)
            tasks.Add(rss.GetPageAsync(page, pageSize, cancellationToken));

        var results = await Task.WhenAll(tasks);

        // Recopilar errores parciales de todos los proveedores
        var errors = new List<string>();
        if (_wpProvider?.LastError is not null) errors.Add(_wpProvider.LastError);
        foreach (var rss in _rssProviders)
            if (rss.LastError is not null) errors.Add(rss.LastError);

        if (errors.Count > 0)
            LastError = string.Join(" | ", errors);

        // Combinar y ordenar por fecha descendente
        var allItems = results
            .SelectMany(r => r.Items)
            .OrderByDescending(a => a.PublishedDate)
            .ToList();

        // HasNextPage: true si AL MENOS UN proveedor tiene más páginas
        var hasNext = results.Any(r => r.HasNextPage);

        // TotalCount: suma si todos lo saben; -1 si alguno no puede determinarlo
        var totals     = results.Select(r => r.Total).ToList();
        var totalCount = totals.Any(t => t < 0) ? -1 : totals.Sum();

        return new ArticlePage
        {
            Items       = allItems,
            CurrentPage = page,
            HasNextPage = hasNext,
            TotalCount  = totalCount
        };
    }
}
