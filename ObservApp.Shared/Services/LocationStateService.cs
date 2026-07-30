using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Implementación de ILocationStateService.
/// Gestiona centralizado el estado de ubicación en toda la aplicación.
/// </summary>
public sealed class LocationStateService : ILocationStateService
{
	private readonly IGeolocationService _geoService;
	private readonly IFavoriteLocationsService _favService;
	private LocationState _currentLocation;
	private string? _lastError;

	public LocationState CurrentLocation => _currentLocation;

	public string? LastError
	{
		get => _lastError;
		private set => _lastError = value;
	}

	public event EventHandler<LocationStateChangedEventArgs>? LocationChanged;

	public LocationStateService(IGeolocationService geoService, IFavoriteLocationsService favService)
	{
		_geoService = geoService ?? throw new ArgumentNullException(nameof(geoService));
		_favService = favService ?? throw new ArgumentNullException(nameof(favService));

		// Inicializar con Madrid como fallback temporal
		// (será actualizado en InitializeAsync)
		_currentLocation = new LocationState(
			Latitude: 40.4168,
			Longitude: -3.7038,
			AltitudeMeters: 650,
			Label: "Madrid (fallback)",
			Source: LocationSourceType.Manual);
	}

	/// <summary>
	/// Inicializa el servicio obteniendo la ubicación GPS real.
	/// Si falla, intenta usar la primera ubicación favorita o Madrid como fallback.
	/// </summary>
	public async Task InitializeAsync()
	{
		_lastError = null;

		// 1. Intentar obtener GPS real
		var gpsLocation = await _geoService.GetCurrentLocationAsync(highAccuracy: true);
		if (gpsLocation != null)
		{
			var newState = new LocationState(
				Latitude: gpsLocation.Latitude,
				Longitude: gpsLocation.Longitude,
				AltitudeMeters: gpsLocation.AltitudeMeters ?? 0,
				Label: $"GPS · {gpsLocation.Latitude:F4}°, {gpsLocation.Longitude:F4}°",
				Source: LocationSourceType.GpsReal);

			RaiseLocationChanged(newState);
			return;
		}

		// 2. Si GPS falla, intentar usar la primera ubicación favorita
		_lastError = _geoService.LastError;
		var favorites = await _favService.GetFavoriteLocationsAsync();

		if (favorites?.Count > 0)
		{
			SetFavoriteLocation(favorites[0]);
			return;
		}

		// 3. Si no hay favoritas, quedarse con Madrid como fallback
		// (ya está en _currentLocation desde el constructor)
	}

	public void SetFavoriteLocation(FavoriteLocation favorite)
	{
		if (favorite == null) throw new ArgumentNullException(nameof(favorite));

		var newState = new LocationState(
			Latitude: favorite.Latitude,
			Longitude: favorite.Longitude,
			AltitudeMeters: favorite.AltitudeMeters,
			Label: $"⭐ {favorite.Name}",
			Source: LocationSourceType.Favorite);

		RaiseLocationChanged(newState);
	}

	public async Task SetGpsLocationAsync()
	{
		_lastError = null;

		var gpsLocation = await _geoService.GetCurrentLocationAsync(highAccuracy: true);
		if (gpsLocation != null)
		{
			var newState = new LocationState(
				Latitude: gpsLocation.Latitude,
				Longitude: gpsLocation.Longitude,
				AltitudeMeters: gpsLocation.AltitudeMeters ?? 0,
				Label: $"GPS · {gpsLocation.Latitude:F4}°, {gpsLocation.Longitude:F4}°",
				Source: LocationSourceType.GpsReal);

			RaiseLocationChanged(newState);
		}
		else
		{
			_lastError = _geoService.LastError;
		}
	}

	public void SetManualLocation(double latitude, double longitude, double altitudeMeters, string label)
	{
		var newState = new LocationState(
			Latitude: latitude,
			Longitude: longitude,
			AltitudeMeters: altitudeMeters,
			Label: label,
			Source: LocationSourceType.Manual);

		RaiseLocationChanged(newState);
	}

	private void RaiseLocationChanged(LocationState newState)
	{
		var oldState = _currentLocation;
		_currentLocation = newState;
		LocationChanged?.Invoke(this, new LocationStateChangedEventArgs
		{
			OldLocation = oldState,
			NewLocation = newState
		});
	}
}
