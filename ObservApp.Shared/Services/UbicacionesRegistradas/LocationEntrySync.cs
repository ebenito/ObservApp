namespace ObservApp.Shared.Services;

using System.Globalization;
using ObservApp.Shared.Models;

/// <summary>
/// Reglas para guardar favoritas y ubicaciones del planificador en <c>location_entries</c>
/// sin duplicar lugares: proximidad, sincronización de favoritas y opciones del selector.
/// </summary>
public static class LocationEntrySync
{
	/// <summary>
	/// Distancia por debajo de la cual dos ubicaciones se consideran el mismo lugar. TiempoUbicacionApp
	/// guarda segundos de arco enteros (hasta ~20 m de error), así que el margen tiene que cubrirlo.
	/// </summary>
	public const double SamePlaceRadiusMeters = 30;

	private const double EarthRadiusMeters = 6_371_008.8;

	// Por debajo de esto un cambio de coordenadas se ignora (ruido de redondeo).
	private const double MovedThresholdMeters = 1;

	/// <summary>Distancia ortodrómica (haversine) en metros.</summary>
	public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = ToRadians(lat2 - lat1);
		var dLon = ToRadians(lon2 - lon1);
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
			Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
	}

	/// <summary>Registro con coordenadas más cercano dentro de <paramref name="radiusMeters"/>, o null.</summary>
	public static LocationEntry? FindNearby(IEnumerable<LocationEntry> entries, double latitude, double longitude, double radiusMeters = SamePlaceRadiusMeters)
	{
		LocationEntry? best = null;
		var bestDistance = double.MaxValue;

		foreach (var entry in entries)
		{
			if (!entry.HasCoordinates())
			{
				continue;
			}

			var distance = DistanceMeters(latitude, longitude, entry.LatitudeDeg!.Value, entry.LongitudeDeg!.Value);
			if (distance <= radiusMeters && distance < bestDistance)
			{
				best = entry;
				bestDistance = distance;
			}
		}

		return best;
	}

	/// <summary>
	/// Calcula qué hay que insertar o actualizar para que cada favorita tenga su fila en
	/// <c>location_entries</c> marcada como favorita:
	/// <list type="bullet">
	/// <item>Si ya hay una fila enlazada (<see cref="LocationEntry.FavoriteId"/>), se actualiza si la favorita cambió.</item>
	/// <item>Si no, y hay un registro a menos de <see cref="SamePlaceRadiusMeters"/> sin favorita, se marca ese.</item>
	/// <item>Si no, se crea una fila nueva con origen <see cref="LocationEntrySources.Favorite"/>.</item>
	/// </list>
	/// No modifica <paramref name="existing"/>: las filas a actualizar son copias.
	/// </summary>
	public static FavoriteSyncPlan PlanFavoriteSync(
		IReadOnlyList<LocationEntry> existing,
		IEnumerable<FavoriteLocation> favorites,
		TimeZoneInfo timeZone)
	{
		// Copias de trabajo: así las decisiones de una favorita las ven las siguientes.
		var working = existing.Select(Clone).ToList();
		var toInsert = new List<LocationEntry>();
		var updated = new Dictionary<Guid, LocationEntry>();

		foreach (var favorite in favorites)
		{
			if (favorite.Id == Guid.Empty)
			{
				continue;
			}

			var linked = working.Where(e => e.FavoriteId == favorite.Id).ToList();
			var handled = false;

			foreach (var entry in linked)
			{
				if (entry.Source == LocationEntrySources.Favorite)
				{
					// Fila propia de la favorita: sigue a la favorita.
					if (!handled && NeedsUpdate(entry, favorite))
					{
						LocationEntryMapper.ApplyLocation(entry, favorite.Name, favorite.Latitude, favorite.Longitude, favorite.AltitudeMeters);
						entry.IsFavorite = true;
						updated[entry.Id] = entry;
					}

					handled = true;
					continue;
				}

				// Registro ajeno (importado o del planificador) marcado como esta favorita.
				var stillHere = entry.HasCoordinates() &&
					DistanceMeters(favorite.Latitude, favorite.Longitude, entry.LatitudeDeg!.Value, entry.LongitudeDeg!.Value) <= SamePlaceRadiusMeters;

				if (stillHere && !handled)
				{
					if (!entry.IsFavorite || entry.AltitudeMeters is null)
					{
						entry.IsFavorite = true;
						entry.AltitudeMeters ??= favorite.AltitudeMeters;
						updated[entry.Id] = entry;
					}

					handled = true;
				}
				else
				{
					// La favorita se movió lejos (o ya tiene otra fila): se desmarca.
					Unmark(entry);
					updated[entry.Id] = entry;
				}
			}

			if (handled)
			{
				continue;
			}

			var nearby = FindNearby(working.Where(e => e.FavoriteId is null), favorite.Latitude, favorite.Longitude);
			if (nearby is not null)
			{
				nearby.IsFavorite = true;
				nearby.FavoriteId = favorite.Id;
				nearby.AltitudeMeters ??= favorite.AltitudeMeters;
				updated[nearby.Id] = nearby;
				continue;
			}

			var created = LocationEntryMapper.CreateEntry(
				favorite.Name,
				favorite.Latitude,
				favorite.Longitude,
				favorite.AltitudeMeters,
				favorite.CreatedAt,
				timeZone,
				LocationEntrySources.Favorite);
			created.IsFavorite = true;
			created.FavoriteId = favorite.Id;
			toInsert.Add(created);
			working.Add(created);
		}

		return new FavoriteSyncPlan(toInsert, updated.Values.ToList(), Array.Empty<Guid>());
	}

	/// <summary>
	/// Qué hacer al borrar una favorita: su fila propia se elimina; los registros importados o del
	/// planificador que tenía marcados se conservan sin la marca.
	/// </summary>
	public static FavoriteSyncPlan PlanFavoriteRemoval(IEnumerable<LocationEntry> existing, Guid favoriteId)
	{
		var toUpdate = new List<LocationEntry>();
		var toDelete = new List<Guid>();

		foreach (var entry in existing.Where(e => e.FavoriteId == favoriteId))
		{
			if (entry.Source == LocationEntrySources.Favorite)
			{
				toDelete.Add(entry.Id);
			}
			else
			{
				var copy = Clone(entry);
				Unmark(copy);
				toUpdate.Add(copy);
			}
		}

		return new FavoriteSyncPlan(Array.Empty<LocationEntry>(), toUpdate, toDelete);
	}

	/// <summary>
	/// Opciones del selector de ubicación del planificador: primero todas las favoritas (por nombre)
	/// y después el resto, de la más reciente a la más antigua. Un registro no favorito a menos de
	/// <see cref="SamePlaceRadiusMeters"/> de una opción ya incluida se omite, para que un lugar
	/// visitado muchas veces aparezca una sola vez.
	/// </summary>
	/// <remarks>
	/// <paramref name="localFavorites"/> cubre el caso en que Supabase no responde o la
	/// sincronización falló: cada favorita sin fila favorita enlazada se añade igualmente.
	/// </remarks>
	public static List<SavedLocationOption> BuildPickerOptions(
		IEnumerable<LocationEntry> entries,
		IEnumerable<FavoriteLocation>? localFavorites = null)
	{
		var comparer = StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
		var withCoordinates = entries.Where(e => e.HasCoordinates()).ToList();

		var favorites = withCoordinates
			.Where(e => e.IsFavorite)
			.Select(e => new SavedLocationOption(e.Id, e.Location, e.LatitudeDeg!.Value, e.LongitudeDeg!.Value, e.AltitudeMeters, true))
			.ToList();

		var linkedFavoriteIds = withCoordinates
			.Where(e => e.IsFavorite && e.FavoriteId.HasValue)
			.Select(e => e.FavoriteId!.Value)
			.ToHashSet();

		favorites.AddRange((localFavorites ?? Enumerable.Empty<FavoriteLocation>())
			.Where(f => !linkedFavoriteIds.Contains(f.Id))
			.Select(f => new SavedLocationOption(f.Id, f.Name, f.Latitude, f.Longitude, f.AltitudeMeters, true)));

		var options = favorites.OrderBy(o => o.Name, comparer).ToList();

		var others = withCoordinates
			.Where(e => !e.IsFavorite)
			.OrderByDescending(e => e.DateTicks)
			.ThenBy(e => e.Location, comparer);

		foreach (var entry in others)
		{
			var lat = entry.LatitudeDeg!.Value;
			var lon = entry.LongitudeDeg!.Value;
			if (!options.Any(o => DistanceMeters(lat, lon, o.Latitude, o.Longitude) <= SamePlaceRadiusMeters))
			{
				options.Add(new SavedLocationOption(entry.Id, entry.Location, lat, lon, entry.AltitudeMeters, false));
			}
		}

		return options;
	}

	private static bool NeedsUpdate(LocationEntry entry, FavoriteLocation favorite) =>
		!entry.IsFavorite ||
		!string.Equals(entry.Location, favorite.Name, StringComparison.Ordinal) ||
		entry.AltitudeMeters != favorite.AltitudeMeters ||
		!entry.HasCoordinates() ||
		DistanceMeters(favorite.Latitude, favorite.Longitude, entry.LatitudeDeg!.Value, entry.LongitudeDeg!.Value) > MovedThresholdMeters;

	private static void Unmark(LocationEntry entry)
	{
		entry.IsFavorite = false;
		entry.FavoriteId = null;
	}

	private static double ToRadians(double degrees) => degrees * Math.PI / 180;

	private static LocationEntry Clone(LocationEntry e) => new()
	{
		Id = e.Id,
		UserId = e.UserId,
		DateTicks = e.DateTicks,
		FechaActual = e.FechaActual,
		HoraLocal = e.HoraLocal,
		HoraUtc = e.HoraUtc,
		Desfase = e.Desfase,
		TipoHorario = e.TipoHorario,
		Location = e.Location,
		Latitude = e.Latitude,
		Longitude = e.Longitude,
		Zona = e.Zona,
		LatitudeDeg = e.LatitudeDeg,
		LongitudeDeg = e.LongitudeDeg,
		IsFavorite = e.IsFavorite,
		Source = e.Source,
		FavoriteId = e.FavoriteId,
		AltitudeMeters = e.AltitudeMeters,
		CreatedAt = e.CreatedAt
	};
}

/// <summary>Cambios que hay que aplicar en <c>location_entries</c>.</summary>
/// <param name="ToInsert">Filas nuevas.</param>
/// <param name="ToUpdate">Filas existentes modificadas (copias).</param>
/// <param name="ToDelete">Identificadores de filas a borrar.</param>
public sealed record FavoriteSyncPlan(
	IReadOnlyList<LocationEntry> ToInsert,
	IReadOnlyList<LocationEntry> ToUpdate,
	IReadOnlyList<Guid> ToDelete)
{
	public bool IsEmpty => ToInsert.Count == 0 && ToUpdate.Count == 0 && ToDelete.Count == 0;
}
