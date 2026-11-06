namespace ObservApp.Services;

using Microsoft.Maui.Storage;
using ObservApp.Shared.Services;

/// <summary>
/// Persistencia segura de credenciales en MAUI.
/// </summary>
public sealed class MauiAuthCredentialStore : IAuthCredentialStore
{
	private const string EmailKey = "auth.supabase.email";
	private const string PasswordKey = "auth.supabase.password";

	public async Task SaveAsync(string email, string password)
	{
		await SecureStorage.Default.SetAsync(EmailKey, email);
		await SecureStorage.Default.SetAsync(PasswordKey, password);
	}

	public async Task<(string? Email, string? Password)> LoadAsync()
	{
		var email = await SecureStorage.Default.GetAsync(EmailKey);
		var password = await SecureStorage.Default.GetAsync(PasswordKey);
		return (email, password);
	}

	public Task ClearAsync()
	{
		SecureStorage.Default.Remove(EmailKey);
		SecureStorage.Default.Remove(PasswordKey);
		return Task.CompletedTask;
	}
}
