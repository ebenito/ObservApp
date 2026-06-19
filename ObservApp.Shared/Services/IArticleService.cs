using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Resultado paginado de artículos.
/// </summary>
public sealed class ArticlePage
{
    /// <summary>Artículos de la página actual.</summary>
    public List<ArticleModel> Items { get; init; } = new();

    /// <summary>Número de página actual (base 1).</summary>
    public int CurrentPage { get; init; }

    /// <summary>True si existe al menos una página siguiente.</summary>
    public bool HasNextPage { get; init; }

    /// <summary>Total de artículos disponibles (-1 si no se puede determinar, p.ej. RSS sin caché).</summary>
    public int TotalCount { get; init; } = -1;
}

/// <summary>
/// Servicio unificado de artículos. Abstrae el origen (API REST de WordPress,
/// feeds RSS/Atom, etc.) y expone una interfaz de paginación consistente
/// independientemente de si el proveedor subyacente soporta paginación nativa.
/// </summary>
public interface IArticleService
{
    /// <summary>
    /// Obtiene una página de artículos de todas las fuentes configuradas,
    /// ordenados por fecha descendente.
    /// </summary>
    /// <param name="page">Página a cargar, base 1.</param>
    /// <param name="pageSize">Artículos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ArticlePage> GetPageAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Último error producido, si lo hay. Null si la última llamada fue exitosa.
    /// Puede contener errores parciales (algunas fuentes fallaron pero otras no).
    /// </summary>
    string? LastError { get; }
}
