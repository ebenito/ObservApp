namespace ObservApp.Shared.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ObservApp.Shared.Models.PageModels;

public sealed class DsoCatalogProvider : IDsoCatalogProvider
{
	public IReadOnlyList<DsoEntry> Catalog => CatalogItems.Value;

	private static readonly Lazy<IReadOnlyList<DsoEntry>> CatalogItems = new(LoadCatalog, true);

	private static IReadOnlyList<DsoEntry> LoadCatalog()
	{
		var assembly = typeof(DsoCatalogProvider).Assembly;
		const string resourceSuffix = "Resources.dso-catalog.json";

		var resourceName = assembly
			.GetManifestResourceNames()
			.FirstOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

		if (resourceName is null)
		{
			return Array.Empty<DsoEntry>();
		}

		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream is null)
		{
			return Array.Empty<DsoEntry>();
		}

		try
		{
			using var reader = new StreamReader(stream);
			var json = reader.ReadToEnd();
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};

			var list = JsonSerializer.Deserialize<List<DsoEntry>>(json, options);
			return list ?? new List<DsoEntry>();
		}
		catch (JsonException)
		{
			return Array.Empty<DsoEntry>();
		}
		catch (IOException)
		{
			return Array.Empty<DsoEntry>();
		}
	}
}
