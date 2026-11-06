namespace ObservApp.Shared.Services;

/// <summary>
/// Configuración de la fuente WordPress de Tubkala que comparten MAUI y Web.
/// </summary>
public static class TubkalaSource
{
	public const string BaseUrl = "https://tubkala.com";
	public const string SourceName = "Tubkala";
	public const string LanguageCode = "es";

	/// <summary>
	/// Slugs de las categorías de Tubkala que se muestran en Señales
	/// (Actividades, Astronomía, Astroturismo, Curiosidades y Equipo).
	/// Lista vacía = todas las categorías. Los slugs que no existan en Tubkala se ignoran
	/// y se avisa en el error de la fuente; comprobarlos en WordPress → Entradas → Categorías.
	/// </summary>
	public static IReadOnlyList<string> CategorySlugs { get; } =
	[
		"actividades",
		"astronomia",
		"astroturismo",
		"curiosidades",
		"equipo",
	];
}
