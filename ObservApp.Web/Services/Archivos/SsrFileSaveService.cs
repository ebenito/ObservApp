namespace ObservApp.Web.Services;

using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;

/// <summary>
/// Stub de <see cref="IFileSaveService"/> para el prerender SSR. No guarda nada.
/// </summary>
public sealed class SsrFileSaveService : IFileSaveService
{
	private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

	public SsrFileSaveService(IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
	{
		_l = localizer;
	}

	public string? LastError { get; private set; }

	public Task<bool> SaveAsync(string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default)
	{
		LastError = _l["Error_SsrUnavailable"];
		return Task.FromResult(false);
	}
}
