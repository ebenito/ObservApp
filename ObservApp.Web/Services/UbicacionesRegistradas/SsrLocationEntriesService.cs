namespace ObservApp.Web.Services;

using Microsoft.Extensions.Localization;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;

/// <summary>
/// Stub de <see cref="ILocationEntriesService"/> para el prerender SSR. No accede a Supabase.
/// </summary>
public sealed class SsrLocationEntriesService : ILocationEntriesService
{
	private readonly IStringLocalizer<ObservApp.Resources.Strings.App> _l;

	public SsrLocationEntriesService(IStringLocalizer<ObservApp.Resources.Strings.App> localizer)
	{
		_l = localizer;
	}

	public string? LastError { get; private set; }

	public Task<List<LocationEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(new List<LocationEntry>());

	public Task<LocationImportPlan?> ImportAsync(IReadOnlyList<LegacyLocationEntry> rows, CancellationToken cancellationToken = default)
	{
		LastError = _l["Error_SsrUnavailable"];
		return Task.FromResult<LocationImportPlan?>(null);
	}

	public Task<bool> DeleteAsync(Guid id)
	{
		LastError = _l["Error_SsrUnavailable"];
		return Task.FromResult(false);
	}
}
