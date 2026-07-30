namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models;
using SupabaseClient = Supabase.Client;
using Supabase.Gotrue;

/// <summary>
/// Implementación de autenticación y persistencia usando Supabase.
/// Implementa IAuthService e IObservationService.
/// Se registra como singleton y se inyecta como dos interfaces.
/// </summary>
public class SupabaseService : IAuthService, IObservationService
{
	private readonly SupabaseClient _supabase;
	private string? _lastError;

	public string? LastError
	{
		get => _lastError;
		private set => _lastError = value;
	}

	public event Action<UserProfile?>? OnAuthStateChanged;

	/// <summary>
	/// Constructor. Inicializa el cliente Supabase con URL y clave anónima.
	/// </summary>
	public SupabaseService(string supabaseUrl, string supabaseAnonKey)
	{
		_supabase = new SupabaseClient(supabaseUrl, supabaseAnonKey);
	}

	/// <summary>
	/// Inicializa el cliente Supabase de forma asincrónica.
	/// Debe llamarse desde Program.cs antes de usar los servicios.
	/// </summary>
	public async Task InitializeAsync()
	{
		try
		{
			await _supabase.InitializeAsync();
		}
		catch (Exception ex)
		{
			LastError = $"Error al inicializar Supabase: {ex.Message}";
			throw;
		}
	}

	#region IAuthService Implementation

	public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
	{
		try
		{
			LastError = null;
			var session = await _supabase.Auth.SignInWithPassword(email, password);

			if (session?.User == null)
				return new AuthResult(false, "No se obtuvo usuario de Supabase", null);

			var userProfile = new UserProfile(
				session.User.Id,
				session.User.Email ?? email,
				session.User.UserMetadata?.ContainsKey("display_name") == true
					? session.User.UserMetadata["display_name"]?.ToString()
					: null
			);

			OnAuthStateChanged?.Invoke(userProfile);
			return new AuthResult(true, null, userProfile);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return new AuthResult(false, ex.Message, null);
		}
	}

	public async Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
	{
		try
		{
			LastError = null;
			var session = await _supabase.Auth.SignUp(email, password, new SignUpOptions
			{
				Data = new Dictionary<string, object> { { "display_name", displayName } }
			});

			if (session?.User == null)
				return new AuthResult(false, "No se obtuvo usuario de Supabase", null);

			var userProfile = new UserProfile(
				session.User.Id,
				session.User.Email ?? email,
				displayName
			);

			OnAuthStateChanged?.Invoke(userProfile);
			return new AuthResult(true, null, userProfile);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return new AuthResult(false, ex.Message, null);
		}
	}

	public async Task SignOutAsync()
	{
		try
		{
			LastError = null;
			await _supabase.Auth.SignOut();
			OnAuthStateChanged?.Invoke(null);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			throw;
		}
	}

	public async Task<UserProfile?> GetCurrentUserAsync()
	{
		try
		{
			LastError = null;
			var user = _supabase.Auth.CurrentUser;
			if (user == null)
				return null;

			return new UserProfile(
				user.Id,
				user.Email ?? string.Empty,
				user.UserMetadata?.ContainsKey("display_name") == true
					? user.UserMetadata["display_name"]?.ToString()
					: null
			);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return null;
		}
	}

	public async Task<bool> IsAuthenticatedAsync()
	{
		try
		{
			LastError = null;
			return _supabase.Auth.CurrentUser != null;
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return false;
		}
	}

	#endregion

	#region IObservationService Implementation

	public async Task<List<ObservationSession>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			LastError = null;
			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return new List<ObservationSession>();
			}

			var result = await _supabase
				.From<ObservationSession>()
				.Where(o => o.UserId == userId)
				.Get();

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
			var userId = _supabase.Auth.CurrentUser?.Id;
			if (string.IsNullOrEmpty(userId))
			{
				LastError = "Usuario no autenticado";
				return false;
			}

			session.UserId = userId;

			if (session.Id == Guid.Empty)
				session.Id = Guid.NewGuid();

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

	#endregion
}
