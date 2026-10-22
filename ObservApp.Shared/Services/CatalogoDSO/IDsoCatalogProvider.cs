namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models.PageModels;

public interface IDsoCatalogProvider
{
	IReadOnlyList<DsoEntry> Catalog { get; }
}
