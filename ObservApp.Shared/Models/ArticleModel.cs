namespace ObservApp.Shared.Models;

/// <summary>
/// Modelo universal de artículo de noticias/blog.
/// Agnóstico al origen: puede venir de la API REST de WordPress,
/// de un feed RSS/Atom, o de cualquier otra fuente futura.
/// </summary>
public sealed class ArticleModel
{
    /// <summary>
    /// Identificador único dentro de su fuente.
    /// WP API: ID numérico del post (como string). RSS: URL del artículo.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Título del artículo, sin HTML.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Resumen breve en texto plano (máx. ~240 caracteres).
    /// WP API: campo excerpt limpiado de HTML. RSS: descripción truncada.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Contenido completo del artículo en HTML.
    /// WP API: campo content.rendered. RSS: content:encoded o description.
    /// </summary>
    public string FullContent { get; init; } = string.Empty;

    /// <summary>URL canónica del artículo en su web de origen.</summary>
    public string SourceUrl { get; init; } = string.Empty;

    /// <summary>Fecha de publicación en UTC.</summary>
    public DateTime PublishedDate { get; init; }

    /// <summary>URL de la imagen destacada, o null si no tiene.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Nombre legible de la fuente, p. ej. "Tubkala" o "NASA JPL".
    /// </summary>
    public string SourceDisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Código ISO de idioma del artículo (2 letras: "es", "en", "fr"...).
    /// Se usa para seleccionar la voz TTS correcta y para el filtro de idioma de la UI.
    /// </summary>
    public string LanguageCode { get; init; } = "es";
}
