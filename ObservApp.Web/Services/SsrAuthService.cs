namespace ObservApp.Web.Services;

using ObservApp.Shared.Services;
using Supabase.Gotrue;

/// <summary>
/// Implementación stub de IAuthService para ASP.NET Core SSR.
/// </summary>
public class SsrAuthService : IAuthService
{
	public string? LastError { get; private set; }

	public User? CurrentUser => null;

	public bool IsAuthenticated => false;

	public event Action<User?>? OnAuthStateChanged;

	public Task<bool> SignUpAsync(string email, string password)
	{
		LastError = "SSR: Registro no disponible";
		return Task.FromResult(false);
	}

	public Task<bool> LoginAsync(string email, string password)
	{
		LastError = "SSR: Autenticación no disponible";
		return Task.FromResult(false);
	}

	public Task<bool> VerifyOtpAsync(string email, string code)
	{
		LastError = "SSR: Verificación OTP no disponible";
		return Task.FromResult(false);
	}

	public Task LogoutAsync()
	{
		LastError = null;
		OnAuthStateChanged?.Invoke(null);
		return Task.CompletedTask;
	}

	public Task<bool> TryRestoreSessionAsync()
	{
		LastError = null;
		return Task.FromResult(false);
	}
}
