namespace ObservApp.Shared.Models;

/// <summary>Tipo de chincheta del mapa; determina el color y la capa en la que se agrupa.</summary>
public enum MapMarkerKind
{
	Favorite,
	Entry
}

/// <summary>Chincheta que pinta el componente <c>ObsMap</c>.</summary>
/// <param name="Id">Identificador único dentro de su tipo.</param>
/// <param name="Kind">Tipo de chincheta.</param>
/// <param name="Latitude">Latitud en grados decimales.</param>
/// <param name="Longitude">Longitud en grados decimales.</param>
/// <param name="Title">Título del popup (texto plano).</param>
/// <param name="Subtitle">Líneas adicionales del popup (texto plano).</param>
public sealed record MapMarker(
	string Id,
	MapMarkerKind Kind,
	double Latitude,
	double Longitude,
	string Title,
	IReadOnlyList<string> Subtitle);
