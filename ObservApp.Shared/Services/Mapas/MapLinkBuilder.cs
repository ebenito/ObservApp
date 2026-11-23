namespace ObservApp.Shared.Services;

using System.Globalization;

/// <summary>
/// URLs públicas de mapas que no necesitan API key (ver MAPAS_SIN_API_KEY.md de
/// TiempoUbicacionApp). Siempre con <see cref="CultureInfo.InvariantCulture"/>: con cultura
/// es-ES el separador decimal sería la coma y la URL dejaría de funcionar.
/// </summary>
public static class MapLinkBuilder
{
	/// <summary>Abre Google Maps (navegador o app) con un marcador en el punto.</summary>
	public static string GoogleMaps(double lat, double lon, int zoom = 15) =>
		$"https://www.google.com/maps?q={Format(lat)},{Format(lon)}&z={zoom}";

	/// <summary>Abre OpenStreetMap con un marcador en el punto.</summary>
	public static string OpenStreetMap(double lat, double lon, int zoom = 15) =>
		$"https://www.openstreetmap.org/?mlat={Format(lat)}&mlon={Format(lon)}#map={zoom}/{Format(lat)}/{Format(lon)}";

	private static string Format(double value) =>
		Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
}
