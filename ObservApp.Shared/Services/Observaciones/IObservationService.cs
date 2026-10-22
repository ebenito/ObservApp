namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models;

/// <summary>
/// Interfaz para servicios de gestión de sesiones de observación.
/// Implementaciones: SupabaseService (MAUI/Web.Client), SsrObservationService (Web SSR).
/// </summary>
public interface IObservationService
{
	/// <summary>
	/// Obtiene todas las sesiones de observación del usuario autenticado.
	/// </summary>
	Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Obtiene una sesión de observación por su identificador.
	/// </summary>
	Task<ObservationSession?> GetByIdAsync(Guid id);

	/// <summary>
	/// Guarda una sesión de observación (crear o actualizar).
	/// </summary>
	Task<bool> SaveAsync(ObservationSession session);

	/// <summary>
	/// Elimina una sesión de observación por su identificador.
	/// </summary>
	Task<bool> DeleteAsync(Guid id);

	/// <summary>
	/// Mensaje de error de la última operación (si la hubo).
	/// </summary>
	string? LastError { get; }
}
