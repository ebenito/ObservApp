namespace ObservApp.Shared.Services;

// Extensión parcial con el método IsValidXml
public partial class RssFeedService
{
	/// <summary>
	/// Detecta si el contenido es XML/RSS/Atom válido.
	/// Comprueba indicadores clave pero es más tolerante que solo buscar &lt;?xml.
	/// </summary>
	private static bool IsValidXml(string content)
	{
		if (string.IsNullOrWhiteSpace(content))
			return false;

		var trimmed = content.TrimStart();

		// Indicadores de HTML (no queremos procesar)
		if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase))
			return false;

		// Indicadores positivos de XML: declaración XML o elemento raíz RSS/feed
		if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("<rss", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("<feed", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("<!-- ", StringComparison.OrdinalIgnoreCase)) // Comentario al inicio
			return true;

		// Si no encuentra indicadores claros, asumir que no es XML válido
		return false;
	}
}
