using System;
using System.Net.Http;
using Supabase.Core.Http;

namespace ObservApp.Web.Client.Services;

/// <summary>
/// Factory HTTP compatible con Blazor WebAssembly para Supabase v8.
/// El navegador no soporta proxy, así que proporcionamos una factory que crea
/// un HttpClient sin intentar configurar proxy.
/// </summary>
public sealed class WebAssemblyHttpClientFactory : IHttpClientFactory
{
	public HttpClient Create(TimeSpan? timeout = null)
	{
		var client = new HttpClient();

		if (timeout.HasValue)
		{
			client.Timeout = timeout.Value;
		}

		return client;
	}

	// Implementación requerida por IHttpClientFactory que espera CreateClient(string).
	// Delegamos al Create existente; el nombre no se utiliza.
	public HttpClient CreateClient(string name)
	{
		return Create();
	}
}
