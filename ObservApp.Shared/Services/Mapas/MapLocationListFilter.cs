namespace ObservApp.Shared.Services;

using System.Globalization;
using System.Text;
using ObservApp.Shared.Models;

/// <summary>Filtro, búsqueda y orden del listado unificado de la página de mapa.</summary>
public static class MapLocationListFilter
{
	public static List<MapLocationItem> Apply(IEnumerable<MapLocationItem> items, MapLocationQuery query)
	{
		var search = Fold(query.Search);

		var filtered = items.Where(i =>
			MatchesSource(i, query.Source) &&
			(search.Length == 0 || Fold(i.Name).Contains(search, StringComparison.Ordinal)) &&
			MatchesDate(i.Date, query));

		var comparer = StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

		// Los elementos sin fecha van siempre al final al ordenar por fecha.
		return query.Sort switch
		{
			MapLocationSort.DateAsc => filtered.OrderBy(i => i.Date.HasValue ? 0 : 1).ThenBy(i => i.Date).ThenBy(i => i.Name, comparer).ToList(),
			MapLocationSort.NameAsc => filtered.OrderBy(i => i.Name, comparer).ThenByDescending(i => i.Date).ToList(),
			MapLocationSort.NameDesc => filtered.OrderByDescending(i => i.Name, comparer).ThenByDescending(i => i.Date).ToList(),
			_ => filtered.OrderBy(i => i.Date.HasValue ? 0 : 1).ThenByDescending(i => i.Date).ThenBy(i => i.Name, comparer).ToList()
		};
	}

	/// <summary>Años presentes en el listado, del más reciente al más antiguo.</summary>
	public static List<int> AvailableYears(IEnumerable<MapLocationItem> items) =>
		items.Where(i => i.Date.HasValue)
			.Select(i => i.Date!.Value.Year)
			.Distinct()
			.OrderByDescending(y => y)
			.ToList();

	private static bool MatchesSource(MapLocationItem item, MapLocationSource source) => source switch
	{
		MapLocationSource.Favorites => item.Kind == MapMarkerKind.Favorite,
		MapLocationSource.Entries => item.Kind == MapMarkerKind.Entry,
		_ => true
	};

	private static bool MatchesDate(DateTime? date, MapLocationQuery query)
	{
		if (!query.HasDateFilter)
		{
			return true;
		}

		if (!date.HasValue)
		{
			return false;
		}

		var d = date.Value;
		return (!query.Year.HasValue || d.Year == query.Year.Value) &&
			(!query.Month.HasValue || d.Month == query.Month.Value) &&
			(!query.Day.HasValue || d.Day == query.Day.Value);
	}

	/// <summary>Minúsculas y sin tildes, para buscar "leon" y encontrar "León".</summary>
	private static string Fold(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (var c in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				sb.Append(char.ToLowerInvariant(c));
			}
		}

		return sb.ToString().Normalize(NormalizationForm.FormC);
	}
}
