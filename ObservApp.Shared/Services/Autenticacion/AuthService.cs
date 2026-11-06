namespace ObservApp.Shared.Services;

using Microsoft.Extensions.Localization;
using Supabase.Gotrue;
using AppStrings = ObservApp.Resources.Strings.App;
using SupabaseClient = Supabase.Client;

/// <summary>
/// Servicio de autenticación directa con Supabase Auth.
/// Los mensajes de <see cref="LastError"/> salen del sistema de traducciones
/// (claves <c>Auth_Error_*</c>); los errores de Supabase se mapean a claves
/// en <see cref="MapAuthErrorKey"/> y, si no se reconocen, se muestran tal cual.
/// </summary>
public sealed class AuthService : IAuthService
{
	private static readonly TimeSpan SupabaseOperationTimeout = TimeSpan.FromSeconds(8);
	private readonly SupabaseClient _supabase;
	private readonly IAuthSessionStore _sessionStore;
	private readonly IAuthCredentialStore _credentialStore;
	private readonly IStringLocalizer<AppStrings> _l;
	private readonly SemaphoreSlim _initializeLock = new(1, 1);
	private bool _initialized;

	// Clave de recurso del último error (null si no se reconoció o no hay error).
	private string? _lastErrorKey;

	public event Action<User?>? OnAuthStateChanged;

	public string? LastError { get; private set; }

	public User? CurrentUser => _supabase.Auth.CurrentUser;

	public bool IsAuthenticated => CurrentUser is not null;

	public AuthService(
		SupabaseClient supabase,
		IAuthSessionStore sessionStore,
		IAuthCredentialStore credentialStore,
		IStringLocalizer<AppStrings> localizer)
	{
		_supabase = supabase;
		_sessionStore = sessionStore;
		_credentialStore = credentialStore;
		_l = localizer;
	}

	public async Task<bool> SignUpAsync(string email, string password)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.SignUp(email, password);
			if (session?.User is null)
			{
				SetError("Auth_Error_SignUpFailed");
				return false;
			}

			// El registro vía OTP no autentica automáticamente al usuario.
			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task<bool> LoginAsync(string email, string password, bool rememberCredentials = false)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.SignInWithPassword(email, password);
			if (session?.User is null)
			{
				SetError("Auth_Error_SignInFailed");
				return false;
			}

			await PersistSessionAsync(session);
			if (rememberCredentials)
			{
				await PersistCredentialsAsync(email, password);
			}
			else
			{
				await _credentialStore.ClearAsync();
			}

			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task<bool> VerifyOtpAsync(string email, string code)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.VerifyOTP(
				email: email,
				token: code,
				type: Supabase.Gotrue.Constants.EmailOtpType.Signup);

			if (session?.User is null)
			{
				SetError("Auth_Error_VerifyCodeFailed");
				return false;
			}

			await PersistSessionAsync(session);
			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task<bool> RequestPasswordResetAsync(string email)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			var sent = await _supabase.Auth.ResetPasswordForEmail(email.Trim());
			if (!sent)
			{
				SetError("Auth_Error_SendRecoveryFailed");
				return false;
			}

			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task<bool> VerifyRecoveryOtpAsync(string email, string code)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			var session = await _supabase.Auth.VerifyOTP(
				email: email.Trim(),
				token: code,
				type: Supabase.Gotrue.Constants.EmailOtpType.Recovery);

			if (session?.User is null)
			{
				SetError("Auth_Error_VerifyCodeFailed");
				return false;
			}

			// Sesión temporal de recuperación: no se persiste hasta cambiar la contraseña.
			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task<bool> UpdatePasswordAsync(string newPassword)
	{
		try
		{
			ClearError();
			await EnsureInitializedAsync();

			if (_supabase.Auth.CurrentSession is null)
			{
				SetError("Auth_Error_NoRecoverySession");
				return false;
			}

			var user = await _supabase.Auth.Update(new UserAttributes { Password = newPassword });
			if (user is null)
			{
				SetError("Auth_Error_ChangePasswordFailed");
				return false;
			}

			// La sesión de recuperación ya es una sesión normal: se persiste y se
			// olvidan las credenciales recordadas, que tenían la contraseña antigua.
			await PersistSessionAsync(_supabase.Auth.CurrentSession);
			await _credentialStore.ClearAsync();
			OnAuthStateChanged?.Invoke(CurrentUser);
			return true;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
			return false;
		}
	}

	public async Task LogoutAsync()
	{
		ClearError();

		try
		{
			await EnsureInitializedAsync();
			await _supabase.Auth.SignOut();
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
		}
		finally
		{
			await _sessionStore.ClearAsync();
			await _credentialStore.ClearAsync();
			OnAuthStateChanged?.Invoke(null);
		}
	}

	public async Task<bool> TryRestoreSessionAsync()
	{
		try
		{
			ClearError();

			var initTask = EnsureInitializedAsync();
			if (await Task.WhenAny(initTask, Task.Delay(SupabaseOperationTimeout)) != initTask)
			{
				SetError("Auth_Error_RestoreTimeout");
				await _sessionStore.ClearAsync();
				return false;
			}

			await initTask;

			var (accessToken, refreshToken) = await _sessionStore.LoadAsync();
			if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
			{
				var session = await _supabase.Auth.SetSession(accessToken, refreshToken);
				if (session?.User is null)
				{
					session = await _supabase.Auth.RefreshSession();
				}

				if (session?.User is not null)
				{
					await PersistSessionAsync(session);
					OnAuthStateChanged?.Invoke(CurrentUser);
					return true;
				}

				await _sessionStore.ClearAsync();
			}

			var (email, password) = await _credentialStore.LoadAsync();
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				return false;
			}

			var loggedIn = await LoginAsync(email, password, rememberCredentials: true);
			if (!loggedIn && string.Equals(_lastErrorKey, "Auth_Error_InvalidCredentials", StringComparison.Ordinal))
			{
				await _credentialStore.ClearAsync();
			}

			return loggedIn;
		}
		catch (Exception ex)
		{
			SetErrorFromException(ex);
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

	private async Task PersistCredentialsAsync(string email, string password)
	{
		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			return;
		}

		await _credentialStore.SaveAsync(email, password);
	}

	// ── Errores ──────────────────────────────────────────────────────────────

	private void ClearError()
	{
		_lastErrorKey = null;
		LastError = null;
	}

	private void SetError(string key)
	{
		_lastErrorKey = key;
		LastError = _l[key];
	}

	private void SetErrorFromException(Exception ex)
	{
		var key = MapAuthErrorKey(ex.Message);
		_lastErrorKey = key;
		LastError = key is null ? ex.Message : _l[key];
	}

	/// <summary>
	/// Traduce el texto de error de Supabase Auth a una clave de recurso
	/// <c>Auth_Error_*</c>. Devuelve null si el mensaje no se reconoce (se mostrará tal cual).
	/// </summary>
	internal static string? MapAuthErrorKey(string? message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return "Auth_Error_Generic";
		}

		if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_InvalidCredentials";
		}

		if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_EmailNotConfirmed";
		}

		if (message.Contains("User already registered", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_AlreadyRegistered";
		}

		if (message.Contains("Password should be at least", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("weak_password", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_WeakPassword";
		}

		if (message.Contains("same_password", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("should be different from the old password", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_SamePassword";
		}

		if (message.Contains("For security purposes", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_RateLimited";
		}

		if (message.Contains("Token has expired", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("expired", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_CodeExpired";
		}

		if (message.Contains("Invalid token", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("Token is invalid", StringComparison.OrdinalIgnoreCase)
			|| message.Contains("otp", StringComparison.OrdinalIgnoreCase))
		{
			return "Auth_Error_CodeInvalid";
		}

		return null;
	}
}
