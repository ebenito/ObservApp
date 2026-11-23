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
		BuildExport(entries, Array.Empty<FavoriteLocation>(), TimeZoneInfo.Local);

	/// <summary>
	/// Exportación combinada de registros y favoritas, ordenada por fecha y numerada 1..n.
	/// Las favoritas se convierten con <see cref="FavoriteToLegacy"/>.
	/// </summary>
	public static List<LegacyLocationEntry> BuildExport(
		IEnumerable<LocationEntry> entries,
		IEnumerable<FavoriteLocation> favorites,
		TimeZoneInfo timeZone)
	{
		var rows = entries
			.OrderBy(e => e.CreatedAt)
			.Select(e => (Ticks: e.DateTicks, Row: ToLegacy(e, 0)))
			.Concat(favorites
				.OrderBy(f => f.CreatedAt)
				.Select(f =>
				{
					var row = FavoriteToLegacy(f, 0, timeZone);
					return (Ticks: long.Parse(row.DateValue!, CultureInfo.InvariantCulture), Row: row);
				}))
			.OrderBy(x => x.Ticks)
			.Select(x => x.Row)
			.ToList();

		for (var i = 0; i < rows.Count; i++)
		{
			rows[i].Id = i + 1;
		}

		return rows;
	}

	/// <summary>
	/// Convierte una favorita en una fila de TiempoUbicacionApp con los mismos formatos que
	/// genera esa app (fecha en español, horas HH:mm:ss, desfase ±hh:mm, coordenadas DMS).
	/// La fecha es la de creación de la favorita en la zona horaria indicada. La altitud se pierde:
	/// TiempoUbicacionApp no la guarda.
	/// </summary>
	public static LegacyLocationEntry FavoriteToLegacy(FavoriteLocation favorite, long id, TimeZoneInfo timeZone)
	{
		var utc = ToUtc(favorite.CreatedAt);
		var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
		var offset = timeZone.GetUtcOffset(utc);
		var spanish = CultureInfo.GetCultureInfo("es-ES");

		return new LegacyLocationEntry
		{
			Id = id,
			DateValue = local.Ticks.ToString(CultureInfo.InvariantCulture),
			DateType = "integer",
			FechaActual = local.ToString("dddd, dd MMMM yyyy", spanish),
			HoraLocal = local.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
			HoraUtc = utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
			Desfase = (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
			TipoHorario = timeZone.IsDaylightSavingTime(local)
				? "Horario de ahorro de luz (verano)"
				: "Horario estándar (invierno)",
			Location = favorite.Name,
			Latitude = CoordinateParser.FormatDms(favorite.Latitude, isLatitude: true),
			Longitude = CoordinateParser.FormatDms(favorite.Longitude, isLatitude: false),
			Zona = 0
		};
	}

	/// <summary>Fecha local de una favorita (se guarda en UTC) para mostrar y filtrar.</summary>
	public static DateTime FavoriteLocalDate(FavoriteLocation favorite, TimeZoneInfo timeZone) =>
		TimeZoneInfo.ConvertTimeFromUtc(ToUtc(favorite.CreatedAt), timeZone);

	// created_at es timestamptz; si llega sin Kind se interpreta como UTC.
	private static DateTime ToUtc(DateTime value) => value.Kind switch
	{
		DateTimeKind.Utc => value,
		DateTimeKind.Local => value.ToUniversalTime(),
		_ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
	};

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
	/// Separa las filas importadas en nuevas y duplicadas. Una fila es duplicada si ya está
	/// guardada (o aparece antes en el mismo archivo) otra con la misma fecha y hora al segundo
	/// y la misma ubicación (ver <see cref="DuplicateKey"/>). El nombre del lugar no cuenta:
	/// renombrarlo en TiempoUbicacionApp no debe provocar un duplicado al reimportar.
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

	/// <summary>
	/// Clave de deduplicación: fecha y hora truncadas al segundo + ubicación. La ubicación se
	/// compara en grados decimales redondeados a 5 decimales (~1 m; TiempoUbicacionApp guarda
	/// segundos de arco enteros, ~30 m), así <c>41°5'35" N</c> y <c>41º 5' 35'' N</c> son la misma.
	/// Si las coordenadas no se pueden interpretar se compara el texto normalizado.
	/// </summary>
	public static string DuplicateKey(LocationEntry entry)
	{
		var secondTicks = entry.DateTicks - entry.DateTicks % TimeSpan.TicksPerSecond;
		var location = entry.HasCoordinates()
			? string.Create(CultureInfo.InvariantCulture, $"{Math.Round(entry.LatitudeDeg!.Value, 5):F5},{Math.Round(entry.LongitudeDeg!.Value, 5):F5}")
			: "txt:" + Normalize(entry.Latitude) + "|" + Normalize(entry.Longitude);

		return secondTicks.ToString(CultureInfo.InvariantCulture) + "\u001F" + location;
	}

	private static string Normalize(string? text) =>
		(text ?? string.Empty).Trim().ToUpperInvariant();
}

/// <summary>Resultado de planificar una importación.</summary>
/// <param name="ToInsert">Registros nuevos que hay que guardar.</param>
/// <param name="Duplicates">Filas descartadas por estar ya guardadas o repetidas en el archivo.</param>
/// <param name="WithoutCoordinates">Registros nuevos cuyas coordenadas no se pudieron interpretar (se guardan, pero no salen en el mapa).</param>
public sealed record LocationImportPlan(IReadOnlyList<LocationEntry> ToInsert, int Duplicates, int WithoutCoordinates);
