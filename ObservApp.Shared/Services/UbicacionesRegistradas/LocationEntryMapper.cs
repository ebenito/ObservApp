namespace ObservApp.Shared.Services;

using System.Globalization;
using ObservApp.Shared.Models;

/// <summary>
/// Conversión entre las filas de TiempoUbicacionApp (<see cref="LegacyLocationEntry"/>)
/// y los registros de Supabase (<see cref="LocationEntry"/>), y planificación de una
/// importación sin duplicados.
/// </summary>
public static class LocationEntryMapper
{
	/// <summary>Convierte una fila importada en un registro listo para guardar.</summary>
	public static LocationEntry FromLegacy(LegacyLocationEntry row)
	{
		var entry = new LocationEntry
		{
			DateTicks = ParseDate(row.DateValue, row.DateType).Ticks,
			FechaActual = row.FechaActual?.Trim() ?? string.Empty,
			HoraLocal = row.HoraLocal?.Trim() ?? string.Empty,
			HoraUtc = row.HoraUtc?.Trim() ?? string.Empty,
			Desfase = row.Desfase?.Trim() ?? string.Empty,
			TipoHorario = row.TipoHorario?.Trim() ?? string.Empty,
			Location = row.Location?.Trim() ?? string.Empty,
			Latitude = row.Latitude?.Trim() ?? string.Empty,
			Longitude = row.Longitude?.Trim() ?? string.Empty,
			Zona = (int)Math.Clamp(row.Zona, int.MinValue, int.MaxValue)
		};

		UpdateDecimalCoordinates(entry);
		return entry;
	}

	/// <summary>Convierte un registro en una fila de TiempoUbicacionApp con el identificador indicado.</summary>
	public static LegacyLocationEntry ToLegacy(LocationEntry entry, long id) => new()
	{
		Id = id,
		DateValue = entry.DateTicks.ToString(CultureInfo.InvariantCulture),
		DateType = "integer",
		FechaActual = entry.FechaActual,
		HoraLocal = entry.HoraLocal,
		HoraUtc = entry.HoraUtc,
		Desfase = entry.Desfase,
		TipoHorario = entry.TipoHorario,
		Location = entry.Location,
		Latitude = entry.Latitude,
		Longitude = entry.Longitude,
		Zona = entry.Zona
	};

	/// <summary>
	/// Filas para exportar en el orden en que TiempoUbicacionApp las habría creado
	/// (de más antigua a más reciente), con identificadores 1..n.
	/// </summary>
	public static List<LegacyLocationEntry> ToLegacyExport(IEnumerable<LocationEntry> entries) =>
		entries
			.OrderBy(e => e.DateTicks)
			.ThenBy(e => e.CreatedAt)
			.Select((e, i) => ToLegacy(e, i + 1))
			.ToList();

	/// <summary>Recalcula <see cref="LocationEntry.LatitudeDeg"/> y <see cref="LocationEntry.LongitudeDeg"/>.</summary>
	public static void UpdateDecimalCoordinates(LocationEntry entry)
	{
		if (CoordinateParser.TryParseLatitude(entry.Latitude, out var lat) &&
			CoordinateParser.TryParseLongitude(entry.Longitude, out var lon))
		{
			entry.LatitudeDeg = lat;
			entry.LongitudeDeg = lon;
		}
		else
		{
			entry.LatitudeDeg = null;
			entry.LongitudeDeg = null;
		}
	}

	/// <summary>
	/// Interpreta la columna <c>Date</c>. sqlite-net la guarda como ticks (entero); si viene
	/// como texto se intenta leer como fecha ISO. Si no se puede interpretar devuelve
	/// <see cref="DateTime.MinValue"/>.
	/// </summary>
	public static DateTime ParseDate(string? value, string? sqliteType)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return DateTime.MinValue;
		}

		var isText = string.Equals(sqliteType, "text", StringComparison.OrdinalIgnoreCase);

		if (!isText && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) &&
			ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks)
		{
			return new DateTime(ticks, DateTimeKind.Unspecified);
		}

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
		{
			return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
		}

		return DateTime.MinValue;
	}

	/// <summary>
	/// Separa las filas importadas en nuevas y duplicadas. Una fila es duplicada si ya existe
	/// (o aparece antes en el mismo archivo) otra con la misma fecha al segundo, el mismo
	/// lugar y las mismas coordenadas en texto.
	/// </summary>
	public static LocationImportPlan PlanImport(IEnumerable<LocationEntry> existing, IEnumerable<LegacyLocationEntry> incoming)
	{
		var keys = new HashSet<string>(existing.Select(DuplicateKey), StringComparer.Ordinal);
		var toInsert = new List<LocationEntry>();
		var duplicates = 0;

		foreach (var row in incoming)
		{
			var entry = FromLegacy(row);
			if (!keys.Add(DuplicateKey(entry)))
			{
				duplicates++;
				continue;
			}

			toInsert.Add(entry);
		}

		return new LocationImportPlan(toInsert, duplicates, toInsert.Count(e => !e.HasCoordinates()));
	}

	/// <summary>Clave de deduplicación de un registro.</summary>
	public static string DuplicateKey(LocationEntry entry)
	{
		var secondTicks = entry.DateTicks - entry.DateTicks % TimeSpan.TicksPerSecond;
		return string.Join('\u001F',
			secondTicks.ToString(CultureInfo.InvariantCulture),
			Normalize(entry.Location),
			Normalize(entry.Latitude),
			Normalize(entry.Longitude));
	}

	private static string Normalize(string? text) =>
		(text ?? string.Empty).Trim().ToUpperInvariant();
}

/// <summary>Resultado de planificar una importación.</summary>
/// <param name="ToInsert">Registros nuevos que hay que guardar.</param>
/// <param name="Duplicates">Filas descartadas por estar ya guardadas o repetidas en el archivo.</param>
/// <param name="WithoutCoordinates">Registros nuevos cuyas coordenadas no se pudieron interpretar (se guardan, pero no salen en el mapa).</param>
public sealed record LocationImportPlan(IReadOnlyList<LocationEntry> ToInsert, int Duplicates, int WithoutCoordinates);
