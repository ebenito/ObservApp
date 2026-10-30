using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Persistencia de credenciales cifradas en navegador para WebAssembly.
/// </summary>
public sealed class BrowserAuthCredentialStore : IAuthCredentialStore
{
	private readonly IJSRuntime _js;

	public BrowserAuthCredentialStore(IJSRuntime js)
	{
		_js = js;
	}

	public async Task SaveAsync(string email, string password)
	{
		try
		{
			await _js.InvokeVoidAsync("ObservApp.authCredentials.save", email, password);
		}
		catch
		{
			// Ignorar si JS no está disponible temporalmente.
		}
	}

	public async Task<(string? Email, string? Password)> LoadAsync()
	{
		try
		{
			var credentials = await _js.InvokeAsync<AuthCredentialsPayload?>("ObservApp.authCredentials.load");
			return (credentials?.Email, credentials?.Password);
		}
		catch
		{
			return (null, null);
		}
	}

	public async Task ClearAsync()
	{
		try
		{
			await _js.InvokeVoidAsync("ObservApp.authCredentials.clear");
		}
		catch
		{
			// Ignorar si JS no está disponible temporalmente.
		}
	}

	private sealed class AuthCredentialsPayload
	{
		public string? Email { get; set; }
		public string? Password { get; set; }
	}
}
