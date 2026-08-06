namespace ObservApp.Shared.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;

public partial class EfemeridesViewModel : ObservableObject, IDisposable
{
	private readonly ILocationStateService _locationState;
	private readonly IFavoriteLocationsService _favoriteLocationsService;

	[ObservableProperty]
	private DateTime fecha = DateTime.Today;

	[ObservableProperty]
	private double latitude;

	[ObservableProperty]
	private double longitude;

	[ObservableProperty]
	private double altitudeMeters;

	[ObservableProperty]
	private bool gpsLoading;

	[ObservableProperty]
	private string? gpsError;

	[ObservableProperty]
	private string? gpsSource;

	[ObservableProperty]
	private ObservableCollection<FavoriteLocation> favoriteLocations = new();

	public event Action? OnStateChanged;

	public EfemeridesViewModel(
		ILocationStateService locationState,
		IFavoriteLocationsService favoriteLocationsService)
	{
		_locationState = locationState;
		_favoriteLocationsService = favoriteLocationsService;
	}

	public async Task InitializeAsync()
	{
		_locationState.LocationChanged += OnLocationStateChanged;

		var favorites = await _favoriteLocationsService.GetFavoriteLocationsAsync();
		FavoriteLocations = new ObservableCollection<FavoriteLocation>(favorites ?? new List<FavoriteLocation>());

		await _locationState.InitializeAsync();
		SyncFromLocationState();
	}

	public async Task UseGpsAsync()
	{
		GpsLoading = true;
		GpsError = null;
		GpsSource = null;
		OnStateChanged?.Invoke();

		await _locationState.SetGpsLocationAsync();
		GpsError = _locationState.LastError;

		GpsLoading = false;
		OnStateChanged?.Invoke();
	}

	public async Task SelectLocationAsync(string value)
	{
		if (value == "manual")
			return;

		if (value == "gps")
		{
			await UseGpsAsync();
			return;
		}

		if (Guid.TryParse(value, out var id))
		{
			var loc = FavoriteLocations.FirstOrDefault(l => l.Id == id);
			if (loc != null)
				_locationState.SetFavoriteLocation(loc);
		}
	}

	public void SetManualLocation(double lat, double lon, double altMeters)
	{
		Latitude = lat;
		Longitude = lon;
		AltitudeMeters = altMeters;
		_locationState.SetManualLocation(lat, lon, altMeters,
			$"Manual · {lat:F4}°, {lon:F4}°");
		OnStateChanged?.Invoke();
	}

	private void SyncFromLocationState()
	{
		Latitude = _locationState.CurrentLocation.Latitude;
		Longitude = _locationState.CurrentLocation.Longitude;
		AltitudeMeters = _locationState.CurrentLocation.AltitudeMeters;
		GpsSource = _locationState.CurrentLocation.Label;
		GpsError = null;
		OnStateChanged?.Invoke();
	}

	private void OnLocationStateChanged(object? sender, LocationStateChangedEventArgs e)
		=> SyncFromLocationState();

	public void Dispose()
	{
		_locationState.LocationChanged -= OnLocationStateChanged;
	}
}
