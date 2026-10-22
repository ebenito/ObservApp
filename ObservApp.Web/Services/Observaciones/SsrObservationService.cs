namespace ObservApp.Web.Services;

using ObservApp.Shared.Models;
using ObservApp.Shared.Services;

/// <summary>
/// Implementación stub de IObservationService para ASP.NET Core SSR.
/// No realiza operaciones reales de persistencia.
/// </summary>
public class SsrObservationService : IObservationService
{
	private string? _lastError;

	public string? LastError => _lastError;

	public Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new List<ObservationSession>());
	}

	public Task<ObservationSession?> GetByIdAsync(Guid id)
	{
		return Task.FromResult<ObservationSession?>(null);
	}

	public Task<bool> SaveAsync(ObservationSession session)
	{
		_lastError = "SSR: Operación de guardado no disponible";
		return Task.FromResult(false);
	}

	public Task<bool> DeleteAsync(Guid id)
	{
		_lastError = "SSR: Operación de eliminación no disponible";
		return Task.FromResult(false);
	}
}
