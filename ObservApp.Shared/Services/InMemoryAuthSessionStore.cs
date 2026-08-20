namespace ObservApp.Shared.Services;

/// <summary>
/// Almacenamiento en memoria para sesión de autenticación en plataformas sin SecureStorage.
/// </summary>
public sealed class InMemoryAuthSessionStore : IAuthSessionStore
{
	private string? _accessToken;
	private string? _refreshToken;

	public Task SaveAsync(string accessToken, string refreshToken)
	{
		_accessToken = accessToken;
		_refreshToken = refreshToken;
		return Task.CompletedTask;
	}

	public Task<(string? AccessToken, string? RefreshToken)> LoadAsync()
	{
		return Task.FromResult((_accessToken, _refreshToken));
	}

	public Task ClearAsync()
	{
		_accessToken = null;
		_refreshToken = null;
		return Task.CompletedTask;
	}
}
