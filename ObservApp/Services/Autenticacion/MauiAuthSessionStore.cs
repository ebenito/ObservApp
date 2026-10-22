namespace ObservApp.Services;

using Microsoft.Maui.Storage;
using ObservApp.Shared.Services;

/// <summary>
/// Persistencia segura de tokens de Supabase en MAUI.
/// </summary>
public sealed class MauiAuthSessionStore : IAuthSessionStore
{
	private const string AccessTokenKey = "auth.supabase.access_token";
	private const string RefreshTokenKey = "auth.supabase.refresh_token";

	public async Task SaveAsync(string accessToken, string refreshToken)
	{
		await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
		await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
	}

	public async Task<(string? AccessToken, string? RefreshToken)> LoadAsync()
	{
		var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
		var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
		return (accessToken, refreshToken);
	}

	public Task ClearAsync()
	{
		SecureStorage.Default.Remove(AccessTokenKey);
		SecureStorage.Default.Remove(RefreshTokenKey);
		return Task.CompletedTask;
	}
}
