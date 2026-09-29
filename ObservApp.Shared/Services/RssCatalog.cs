namespace ObservApp.Shared.Services;

public sealed record RssCatalogEntry(RssSource Source, string LanguageCode);

public static class RssCatalog
{
	public static IReadOnlyList<RssCatalogEntry> Sources { get; } =
	[
		// Español
		new(new RssSource("astrobit", "Astrobitácora", "https://www.astrobitacora.com/feed/", IsBuiltIn: true), "es"),
		new(new RssSource("astrobites", "Astrobites ES", "https://astrobitos.org/feed/", IsBuiltIn: true), "es"),
		new(new RssSource("esaes", "ESA España", "https://www.esa.int/rssfeed/Spain", IsBuiltIn: true), "es"),
		new(new RssSource("nasaes", "Universo curioso de la NASA", "https://feeds.megaphone.fm/nationalaeronauticsandspaceadministration5412631684", IsBuiltIn: true), "es"),

		// Inglés
		new(new RssSource("nasa", "NASA", "https://www.nasa.gov/feed/", IsBuiltIn: true), "en"),
		new(new RssSource("nasaimg", "NASA Image of the Day", "https://www.nasa.gov/feeds/iotd-feed/", IsBuiltIn: true), "en"),
		new(new RssSource("esa", "ESA", "http://www.esa.int/rssfeed/Our_Activities/Space_Science", IsBuiltIn: true), "en"),
		new(new RssSource("eso", "ESO", "https://www.eso.org/public/blog/feed/", IsBuiltIn: true), "en"),
		new(new RssSource("skytel", "Sky & Telescope", "https://skyandtelescope.org/feed/", IsBuiltIn: true), "en"),
		new(new RssSource("astromag", "Astronomy Magazine", "https://www.astronomy.com/feed/", IsBuiltIn: true), "en"),
	];
}
