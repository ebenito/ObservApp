namespace ObservApp.Shared.Services;

using Microsoft.Extensions.Localization;
using ObservApp.Shared.Models;
using Supabase;
using static Supabase.Postgrest.Constants;
using AppStrings = ObservApp.Resources.Strings.App;

/// <summary>
/// Implementación de <see cref="ILocationEntriesService"/> sobre la tabla
/// <c>location_entries</c> de Supabase. Común a MAUI y Web.
/// </summary>
public class SupabaseLocationEntriesService : ILocationEntriesService
{
	// PostgREST limita cada respuesta a 1000 filas por defecto.
	private const int PageSize = 1000;
	private const int InsertBatchSize = 500;

	private readonly Client _supabase;
	private readonly IStringLocalizer<AppStrings> _l;
	private readonly SemaphoreSlim _initializeLock = new(1, 1);
	private bool _initialized;

	public SupabaseLocationEntriesService(Client supabase, IStringLocalizer<AppStrings> localizer)
	{
		_supabase = supabase;
		_l = localizer;
	}

	public string? LastError { get; private set; }

	public async Task<List<LocationEntry>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			LastError = null;
			var userId = await GetUserIdAsync();
			if (userId is null)
			{
				return new List<LocationEntry>();
			}

			return await GetAllForUserAsync(userId, cancellationToken);
		}
		catch (Exception ex)
		{
			LastError = _l["MapLoc_Error_Load", ex.Message];
			return new List<LocationEntry>();
		}
	}

	public async Task<LocationImportPlan?> ImportAsync(IReadOnlyList<LegacyLocationEntry> rows, CancellationToken cancellationToken = default)
	{
		try
		{
			LastError = null;
			var userId = await GetUserIdAsync();
			if (userId is null)
			{
				return null;
			}

			var existing = await GetAllForUserAsync(userId, cancellationToken);
			var plan = LocationEntryMapper.PlanImport(existing, rows);

			foreach (var entry in plan.ToInsert)
			{
				entry.UserId = userId;
				entry.CreatedAt = DateTime.UtcNow;
			}

			foreach (var batch in plan.ToInsert.Chunk(InsertBatchSize))
			{
				cancellationToken.ThrowIfCancellationRequested();
				await _supabase.From<LocationEntry>().Insert(batch.ToList(), cancellationToken: cancellationToken);
			}

			return plan;
		}
		catch (Exception ex)
		{
			LastError = _l["MapLoc_Error_Import", ex.Message];
			return null;
		}
	}

	public async Task<bool> DeleteAsync(Guid id)
	{
		try
		{
			LastError = null;
			var userId = await GetUserIdAsync();
			if (userId is null)
			{
				return false;
			}

			await _supabase
				.From<LocationEntry>()
				.Where(e => e.Id == id && e.UserId == userId)
				.Delete();

			return true;
		}
		catch (Exception ex)
		{
			LastError = _l["MapLoc_Error_Delete", ex.Message];
			return false;
		}
	}

	private async Task<List<LocationEntry>> GetAllForUserAsync(string userId, CancellationToken cancellationToken)
	{
		var all = new List<LocationEntry>();
		for (var from = 0; ; from += PageSize)
		{
			var page = await _supabase
				.From<LocationEntry>()
				.Where(e => e.UserId == userId)
				.Order(e => e.DateTicks, Ordering.Descending)
				.Order(e => e.Id, Ordering.Ascending)
				.Range(from, from + PageSize - 1)
				.Get(cancellationToken: cancellationToken);

			var models = page?.Models ?? new List<LocationEntry>();
			all.AddRange(models);
			if (models.Count < PageSize)
			{
				return all;
			}
		}
	}

	private async Task<string?> GetUserIdAsync()
	{
		await EnsureInitializedAsync();
		var userId = _supabase.Auth.CurrentUser?.Id;
		if (string.IsNullOrEmpty(userId))
		{
			LastError = _l["Obs_Error_NotAuthenticated"];
			return null;
		}

		return userId;
	}

	private async Task EnsureInitializedAsync()
	{
		if (_initialized)
		{
			return;
		}

		await _initializeLock.WaitAsync();
		try
		{
			if (!_initialized)
			{
				await _supabase.InitializeAsync();
				_initialized = true;
			}
		}
		finally
		{
			_initializeLock.Release();
		}
	}
}
