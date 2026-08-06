namespace ObservApp.Shared.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using ObservApp.Shared.State;

public partial class HomeViewModel : ObservableObject, IDisposable
{
	private readonly ILocationStateService _locationState;
	private readonly IFavoriteLocationsService _favoriteLocationsService;
	private readonly IObservationService _observationService;
	private readonly AppState _appState;

	[ObservableProperty]
	private double latitude;

	[ObservableProperty]
	private double longitude;

	[ObservableProperty]
	private double altitudeMeters;

	[ObservableProperty]
	private string locationLabel = string.Empty;

	[ObservableProperty]
	private bool gpsLoading;

	[ObservableProperty]
	private string? gpsError;

	[ObservableProperty]
	private ObservableCollection<FavoriteLocation> favoriteLocations = new();

	[ObservableProperty]
	private int sessionCount;

	[ObservableProperty]
	private bool showRecentSessions;

	[ObservableProperty]
	private ObservableCollection<ObservationSession> recentSessions = new();

	public event Action? OnStateChanged;

	public HomeViewModel(
		ILocationStateService locationState,
		IFavoriteLocationsService favoriteLocationsService,
		IObservationService observationService,
		AppState appState)
	{
		_locationState = locationState;
		_favoriteLocationsService = favoriteLocationsService;
		_observationService = observationService;
		_appState = appState;
	}

	public async Task InitializeAsync()
	{
		_locationState.LocationChanged += OnLocationStateChanged;
		_appState.OnAuthChanged += OnAppAuthChanged;

		var favorites = await _favoriteLocationsService.GetFavoriteLocationsAsync();
		FavoriteLocations = new ObservableCollection<FavoriteLocation>(favorites ?? new List<FavoriteLocation>());

		await _locationState.InitializeAsync();
		SyncFromLocationState();
		await LoadRecentSessionsAsync();
	}

	public async Task UseGpsAsync()
	{
		GpsLoading = true;
		GpsError = null;
		OnStateChanged?.Invoke();

		await _locationState.SetGpsLocationAsync();
		GpsError = _locationState.LastError;

		GpsLoading = false;
		OnStateChanged?.Invoke();
	}

	public async Task SelectLocationAsync(string? value)
	{
		if (string.IsNullOrWhiteSpace(value) || value == "manual")
			return;

		if (value == "gps")
		{
			await UseGpsAsync();
			return;
		}

		if (Guid.TryParse(value, out var id))
		{
			var loc = FavoriteLocations.FirstOrDefault(x => x.Id == id);
			if (loc != null)
				_locationState.SetFavoriteLocation(loc);
		}
	}

	public async Task LoadRecentSessionsAsync()
	{
		ShowRecentSessions = false;
		RecentSessions = new ObservableCollection<ObservationSession>();
		SessionCount = 0;

		if (!_appState.IsAuthenticated)
			return;

		try
		{
			var sessions = await _observationService.GetAllAsync();
			SessionCount = sessions.Count;
			RecentSessions = new ObservableCollection<ObservationSession>(
				sessions.OrderByDescending(s => s.Date).Take(3));
			ShowRecentSessions = true;
		}
		catch
		{
			ShowRecentSessions = false;
		}
		finally
		{
			OnStateChanged?.Invoke();
		}
	}

	private void SyncFromLocationState()
	{
		Latitude = _locationState.CurrentLocation.Latitude;
		Longitude = _locationState.CurrentLocation.Longitude;
		AltitudeMeters = _locationState.CurrentLocation.AltitudeMeters;
		LocationLabel = _locationState.CurrentLocation.Label;
		GpsError = null;
		OnStateChanged?.Invoke();
	}

	private void OnLocationStateChanged(object? sender, LocationStateChangedEventArgs e)
		=> SyncFromLocationState();

	private void OnAppAuthChanged()
		=> _ = LoadRecentSessionsAsync();

	public void Dispose()
	{
		_locationState.LocationChanged -= OnLocationStateChanged;
		_appState.OnAuthChanged -= OnAppAuthChanged;
	}
}
