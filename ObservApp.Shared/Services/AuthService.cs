namespace ObservApp.Shared.Services;

using Supabase.Gotrue;
using SupabaseClient = Supabase.Client;

/// <summary>
/// Servicio de autenticación directa con Supabase Auth.
/// </summary>
public sealed class AuthService : IAuthService
{
	private readonly SupabaseClient _supabase;
	private readonly IAuthSessionStore _sessionStore;
	private readonly SemaphoreSlim _initializeLock = new(1, 1);
	private bool _initialized;

	public event Action<User?>? OnAuthStateChanged;

	public string? LastError { get; private set; }

	public User? CurrentUser => _supabase.Auth.CurrentUser;

	public bool IsAuthenticated => CurrentUser is not null;

	public AuthService(SupabaseClient supabase, IAuthSessionStore sessionStore)
	{
		_supabase = supabase;
		_sessionStore = sessionStore;
	}

	public async Task<bool> SignUpAsync(string email, string password)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.SignUp(email, password);
			if (session?.User is null)
			{
				LastError = "No se pudo crear la cuenta.";
				return false;
			}

			await PersistSessionAsync(session);
			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			LastError = NormalizeAuthError(ex.Message);
			return false;
		}
	}

	public async Task<bool> LoginAsync(string email, string password)
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.SignInWithPassword(email, password);
			if (session?.User is null)
			{
				LastError = "No se pudo iniciar sesión.";
				return false;
			}

			await PersistSessionAsync(session);
			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			LastError = NormalizeAuthError(ex.Message);
			return false;
		}
	}

	public async Task LogoutAsync()
	{
		LastError = null;

		try
		{
			await EnsureInitializedAsync();
			await _supabase.Auth.SignOut();
		}
		catch (Exception ex)
		{
			LastError = NormalizeAuthError(ex.Message);
		}
		finally
		{
			await _sessionStore.ClearAsync();
			OnAuthStateChanged?.Invoke(null);
		}
	}

	public async Task<bool> TryRestoreSessionAsync()
	{
		try
		{
			LastError = null;
			await EnsureInitializedAsync();

			var (accessToken, refreshToken) = await _sessionStore.LoadAsync();
			if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
			{
				return false;
			}

			var session = await _supabase.Auth.SetSession(accessToken, refreshToken);
			if (session?.User is null)
			{
				session = await _supabase.Auth.RefreshSession();
			}

			if (session?.User is null)
			{
				await _sessionStore.ClearAsync();
				return false;
			}

			await PersistSessionAsync(session);
			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			LastError = NormalizeAuthError(ex.Message);
			await _sessionStore.ClearAsync();
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

	private async Task PersistSessionAsync(Session? session)
	{
		var accessToken = session?.AccessToken;
		var refreshToken = session?.RefreshToken;

		if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
		{
			return;
		}

		await _sessionStore.SaveAsync(accessToken, refreshToken);
	}

	private static string NormalizeAuthError(string? message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return "No se pudo completar la autenticación.";
		}

		if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
		{
			return "Credenciales inválidas.";
		}

		if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
		{
			return "Debes confirmar tu correo antes de iniciar sesión.";
		}

		if (message.Contains("User already registered", StringComparison.OrdinalIgnoreCase))
		{
			return "El usuario ya está registrado.";
		}

		return message;
	}
}
