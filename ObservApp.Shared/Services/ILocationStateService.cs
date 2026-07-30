using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Estado de ubicación actual: si es GPS real, ubicación favorita, o manual.
/// </summary>
public record LocationState(
	double Latitude,
	double Longitude,
	double AltitudeMeters,
	string Label,
	LocationSourceType Source);

/// <summary>
/// Tipo de origen de la ubicación actual.
/// </summary>
public enum LocationSourceType
{
	/// <summary>GPS real del dispositivo.</summary>
	GpsReal,
	/// <summary>Una de las ubicaciones favoritas guardadas.</summary>
	Favorite,
	/// <summary>Ubicación introducida manualmente o por defecto.</summary>
	Manual
}

/// <summary>
/// Servicio centralizado que gestiona el estado compartido de ubicación en toda la app.
/// Se inicializa al arrancar buscando la ubicación GPS real del cliente.
/// Permite cambiar entre ubicación GPS actual y ubicaciones favoritas guardadas.
/// </summary>
public interface ILocationStateService
{
	/// <summary>
	/// Ubicación actual (GPS real, favorita, o manual).
	/// </summary>
	LocationState CurrentLocation { get; }

	/// <summary>
	/// Se dispara cuando cambio la ubicación actual (por GPS, favorita o manual).
	/// </summary>
	event EventHandler<LocationStateChangedEventArgs>? LocationChanged;

	/// <summary>
	/// Inicializa el servicio obteniendo la ubicación GPS real del dispositivo.
	/// Se debe llamar al iniciar la app. Si falla, usa la primera ubicación favorita
	/// o Madrid como fallback.
	/// </summary>
	Task InitializeAsync();

	/// <summary>
	/// Cambia la ubicación actual a una de las favoritas.
	/// </summary>
	void SetFavoriteLocation(FavoriteLocation favorite);

	/// <summary>
	/// Vuelve a obtener la ubicación GPS actual del dispositivo.
	/// </summary>
	Task SetGpsLocationAsync();

	/// <summary>
	/// Establece una ubicación manual (para casos especiales como búsqueda, edición, etc.).
	/// </summary>
	void SetManualLocation(double latitude, double longitude, double altitudeMeters, string label);

	/// <summary>
	/// Último error al obtener GPS, o null si no hay error.
	/// </summary>
	string? LastError { get; }
}

/// <summary>
/// Argumentos para el evento LocationChanged.
/// </summary>
public class LocationStateChangedEventArgs : EventArgs
{
	public LocationState OldLocation { get; init; }
	public LocationState NewLocation { get; init; }
}
