namespace ObservApp.Web.Services;

using ObservApp.Shared.Services;

/// <summary>
/// Implementación stub de IAuthService para ASP.NET Core SSR.
/// No realiza operaciones reales de autenticación.
/// IsAuthenticatedAsync siempre devuelve false.
/// GetCurrentUserAsync siempre devuelve null.
/// </summary>
public class SsrAuthService : IAuthService
{
	public event Action<UserProfile?>? OnAuthStateChanged;

	public Task<AuthResult> SignInWithEmailAsync(string email, string password)
	{
		return Task.FromResult(new AuthResult(false, "SSR: Autenticación no disponible", null));
	}

	public Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
	{
		return Task.FromResult(new AuthResult(false, "SSR: Registro no disponible", null));
	}

	public Task SignOutAsync()
	{
		return Task.CompletedTask;
	}

	public Task<UserProfile?> GetCurrentUserAsync()
	{
		return Task.FromResult<UserProfile?>(null);
	}

	public Task<bool> IsAuthenticatedAsync()
	{
		return Task.FromResult(false);
	}
}
