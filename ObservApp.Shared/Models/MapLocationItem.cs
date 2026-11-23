namespace ObservApp.Shared.Models;

/// <summary>
/// Elemento del listado unificado de la página de mapa: una ubicación favorita o un registro
/// de TiempoUbicacionApp.
/// </summary>
/// <param name="Id">Identificador del favorito o del registro.</param>
/// <param name="Kind">Origen; determina el color de la chincheta.</param>
/// <param name="Name">Nombre del lugar (puede estar vacío en registros).</param>
/// <param name="Date">Fecha local: la del registro o la de creación del favorito. Nula si no se conoce.</param>
/// <param name="Latitude">Latitud decimal; nula si no se pudo interpretar.</param>
/// <param name="Longitude">Longitud decimal; nula si no se pudo interpretar.</param>
/// <param name="CoordinatesText">Coordenadas tal como se muestran en el listado.</param>
public sealed record MapLocationItem(
	Guid Id,
	MapMarkerKind Kind,
	string Name,
	DateTime? Date,
	double? Latitude,
	double? Longitude,
	string CoordinatesText)
{
	public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
}

/// <summary>Origen que se muestra en el listado.</summary>
public enum MapLocationSource
{
	All,
	Favorites,
	Entries
}

/// <summary>Orden del listado.</summary>
public enum MapLocationSort
{
	DateDesc,
	DateAsc,
	NameAsc,
	NameDesc
}

/// <summary>
/// Filtros del listado. Año, mes y día se combinan: solo el año da "registros de 2025",
/// solo el mes da "agosto de cualquier año", los tres dan una fecha exacta.
/// </summary>
public sealed record MapLocationQuery(
	string? Search = null,
	MapLocationSource Source = MapLocationSource.All,
	int? Year = null,
	int? Month = null,
	int? Day = null,
	MapLocationSort Sort = MapLocationSort.DateDesc)
{
	public bool HasDateFilter => Year.HasValue || Month.HasValue || Day.HasValue;

	public bool IsDefault => string.IsNullOrWhiteSpace(Search) && Source == MapLocationSource.All && !HasDateFilter;
}
