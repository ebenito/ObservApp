using Microsoft.JSInterop;
using ObservApp.Shared.Services;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Persistencia durable de la sesión de Supabase en localStorage para Blazor WebAssembly.
/// Soluciona que v8 deje de guardar la sesión por defecto y que el cliente web pierda el usuario
/// al recargar la página.
/// </summary>
public sealed class BrowserAuthSessionStore : IAuthSessionStore
{
	private const string AccessTokenKey = "auth.supabase.access_token";
	private const string RefreshTokenKey = "auth.supabase.refresh_token";

	private readonly IJSRuntime _js;

	public BrowserAuthSessionStore(IJSRuntime js)
	{
		_js = js;
	}

	public async Task SaveAsync(string accessToken, string refreshToken)
	{
		try
		{
			await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
			await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
		}
		catch
		{
			// localStorage no disponible en prerender/SSR; la sesión se recupera cuando el cliente
			// esté realmente montado en el navegador.
		}
	}

	public async Task<(string? AccessToken, string? RefreshToken)> LoadAsync()
	{
		try
		{
			var accessToken = await _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
			var refreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
			return (accessToken, refreshToken);
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
			await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
			await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
		}
		catch
		{
			// Ignorar si el navegador no está disponible todavía.
		}
	}
}
