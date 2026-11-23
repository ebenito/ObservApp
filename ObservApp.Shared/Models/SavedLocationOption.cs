namespace ObservApp.Shared.Models;

/// <summary>
/// Ubicación que se ofrece en el selector del planificador, obtenida de <c>location_entries</c>.
/// </summary>
/// <param name="Id">Identificador del registro.</param>
/// <param name="Name">Nombre del lugar (puede estar vacío).</param>
/// <param name="Latitude">Latitud decimal.</param>
/// <param name="Longitude">Longitud decimal.</param>
/// <param name="AltitudeMeters">Altitud en metros si se conoce.</param>
/// <param name="IsFavorite">True si está marcada como favorita.</param>
public sealed record SavedLocationOption(
	Guid Id,
	string Name,
	double Latitude,
	double Longitude,
	double? AltitudeMeters,
	bool IsFavorite);
