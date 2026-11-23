namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models;

/// <summary>
/// Registros de ubicación compatibles con TiempoUbicacionApp, guardados en Supabase
/// (<c>location_entries</c>). Requiere sesión iniciada.
/// </summary>
public interface ILocationEntriesService
{
	/// <summary>Último error, ya traducido. Nulo si la última operación fue bien.</summary>
	string? LastError { get; }

	/// <summary>Registros del usuario, del más reciente al más antiguo.</summary>
	Task<List<LocationEntry>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Importa filas de TiempoUbicacionApp descartando las que ya existen.
	/// Devuelve null si falla (ver <see cref="LastError"/>).
	/// </summary>
	Task<LocationImportPlan?> ImportAsync(IReadOnlyList<LegacyLocationEntry> rows, CancellationToken cancellationToken = default);

	Task<bool> DeleteAsync(Guid id);

	/// <summary>
	/// Asegura que cada favorita tiene su fila en <c>location_entries</c> marcada como favorita
	/// (ver <see cref="LocationEntrySync.PlanFavoriteSync"/>). Solo trata las favoritas indicadas.
	/// Devuelve el número de filas creadas o modificadas, o null si falla (ver <see cref="LastError"/>).
	/// </summary>
	Task<int?> SyncFavoritesAsync(IReadOnlyList<FavoriteLocation> favorites, CancellationToken cancellationToken = default);

	/// <summary>
	/// Quita la favorita de <c>location_entries</c>: borra su fila propia y desmarca los registros
	/// que tenía marcados. False si falla.
	/// </summary>
	Task<bool> RemoveFavoriteAsync(Guid favoriteId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Guarda una ubicación usada en el planificador, salvo que ya exista otra a menos de
	/// <see cref="LocationEntrySync.SamePlaceRadiusMeters"/>. Devuelve la fila nueva o la existente,
	/// o null si falla.
	/// </summary>
	Task<LocationEntry?> AddPlannerLocationAsync(string name, double latitude, double longitude, double? altitudeMeters, CancellationToken cancellationToken = default);
}
