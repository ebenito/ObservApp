namespace ObservApp.Web.Services;

using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;
using Supabase.Gotrue;

/// <summary>
/// Implementación stub de IAuthService para ASP.NET Core SSR.
/// </summary>
public class SsrAuthService : IAuthService
{
	private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

	public SsrAuthService(IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
	{
		_l = localizer;
	}

	public string? LastError { get; private set; }

	public User? CurrentUser => null;

	public bool IsAuthenticated => false;

	public event Action<User?>? OnAuthStateChanged;

	public Task<bool> SignUpAsync(string email, string password) => Unavailable();

	public Task<bool> LoginAsync(string email, string password, bool rememberCredentials = false) => Unavailable();

	public Task<bool> VerifyOtpAsync(string email, string code) => Unavailable();

	public Task<bool> RequestPasswordResetAsync(string email) => Unavailable();

	public Task<bool> VerifyRecoveryOtpAsync(string email, string code) => Unavailable();

	public Task<bool> UpdatePasswordAsync(string newPassword) => Unavailable();

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

	private Task<bool> Unavailable()
	{
		LastError = _l["Error_SsrUnavailable"];
		return Task.FromResult(false);
	}
}
