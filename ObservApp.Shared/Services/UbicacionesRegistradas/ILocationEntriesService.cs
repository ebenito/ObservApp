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
}
