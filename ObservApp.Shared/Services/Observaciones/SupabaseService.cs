namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models;
using Supabase;

/// <summary>
/// Implementación de persistencia de sesiones de observación usando Supabase.
/// </summary>
public class SupabaseService : IObservationService
{
	private readonly Client _supabase;
	private readonly SemaphoreSlim _initializeLock = new(1, 1);
	private bool _initialized;
	private string? _lastError;

	public string? LastError
	{
		get => _lastError;
		private set => _lastError = value;
	}

	public SupabaseService(Client supabase)
	{
		_supabase = supabase;
	}

	public async Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return new List<ObservationSession>();
			}

			var result = await _supabase
				.From<ObservationSession>()
				.Where(o => o.UserId == userId)
				.Get(cancellationToken: cancellationToken);

			return result?.Models ?? new List<ObservationSession>();
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return new List<ObservationSession>();
		}
	}

	public async Task<ObservationSession?> GetByIdAsync(Guid id)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return null;
			}

			var result = await _supabase
				.From<ObservationSession>()
				.Where(o => o.Id == id && o.UserId == userId)
				.Single();

			return result;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return null;
		}
	}

	public async Task<bool> SaveAsync(ObservationSession session)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return false;
			}

			session.UserId = userId;

			if (session.Id == Guid.Empty)
			{
				session.Id = Guid.NewGuid();
			}

			var existing = await _supabase
				.From<ObservationSession>()
				.Where(o => o.Id == session.Id && o.UserId == userId)
				.Single();

			if (existing != null)
			{
				await _supabase
					.From<ObservationSession>()
					.Where(o => o.Id == session.Id)
					.Update(session);
			}
			else
			{
				await _supabase
					.From<ObservationSession>()
					.Insert(session);
			}

			return true;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return false;
		}
	}

	public async Task<bool> DeleteAsync(Guid id)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return false;
			}

			await _supabase
				.From<ObservationSession>()
				.Where(o => o.Id == id && o.UserId == userId)
				.Delete();

			return true;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return false;
		}
	}

	private async Task EnsureInitializedAsync()
	{
		if (_initialized)
		{
			return;
		}

		await _initializeLock.WaitAsync();
		try
		{
			if (_initialized)
			{
				return;
			}

			await _supabase.InitializeAsync();
			_initialized = true;
		}
		finally
		{
			_initializeLock.Release();
		}
	}
}
