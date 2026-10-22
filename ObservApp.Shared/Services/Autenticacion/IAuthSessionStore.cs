namespace ObservApp.Shared.Services;

/// <summary>
/// Contrato para persistencia de tokens de sesión de Supabase.
/// </summary>
public interface IAuthSessionStore
{
	Task SaveAsync(string accessToken, string refreshToken);
	Task<(string? AccessToken, string? RefreshToken)> LoadAsync();
	Task ClearAsync();
}
