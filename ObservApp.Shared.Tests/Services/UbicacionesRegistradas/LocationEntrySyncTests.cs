using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.UbicacionesRegistradas;

public sealed class LocationEntrySyncTests
{
	private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

	// ~1 m de latitud = 1/111 195 grados.
	private const double MeterLat = 1.0 / 111_195;

	private static FavoriteLocation Favorite(string name, double lat, double lon, double alt = 0) => new()
	{
		Id = Guid.NewGuid(),
		Name = name,
		Latitude = lat,
		Longitude = lon,
		AltitudeMeters = alt,
		CreatedAt = new DateTime(2026, 5, 2, 16, 2, 27, DateTimeKind.Utc)
	};

	private static LocationEntry Imported(string name, double lat, double lon, long ticks = 638_899_171_523_191_677) => new()
	{
		Id = Guid.NewGuid(),
		Location = name,
		DateTicks = ticks,
		LatitudeDeg = lat,
		LongitudeDeg = lon
	};

	[Fact]
	public void DistanceMeters_OneArcSecondOfLatitude_IsAbout31m()
	{
		Assert.Equal(30.9, LocationEntrySync.DistanceMeters(40, -3, 40 + 1.0 / 3600, -3), 1);
	}

	[Fact]
	public void FindNearby_ReturnsClosestWithinRadius()
	{
		var far = Imported("Lejos", 40 + 100 * MeterLat, -3);
		var near = Imported("Cerca", 40 + 20 * MeterLat, -3);
		var nearest = Imported("Muy cerca", 40 + 5 * MeterLat, -3);

		Assert.Same(nearest, LocationEntrySync.FindNearby(new[] { far, near, nearest }, 40, -3));
		Assert.Null(LocationEntrySync.FindNearby(new[] { far }, 40, -3));
	}

	[Fact]
	public void FindNearby_IgnoresEntriesWithoutCoordinates()
	{
		var noCoords = new LocationEntry { Location = "X" };

		Assert.Null(LocationEntrySync.FindNearby(new[] { noCoords }, 40, -3));
	}

	[Fact]
	public void PlanFavoriteSync_NewPlace_InsertsFavoriteRow()
	{
		var teide = Favorite("Teide", 28.2724, -16.6425, 2150);

		var plan = LocationEntrySync.PlanFavoriteSync(Array.Empty<LocationEntry>(), new[] { teide }, Madrid);

		var row = Assert.Single(plan.ToInsert);
		Assert.Empty(plan.ToUpdate);
		Assert.True(row.IsFavorite);
		Assert.Equal(LocationEntrySources.Favorite, row.Source);
		Assert.Equal(teide.Id, row.FavoriteId);
		Assert.Equal("Teide", row.Location);
		Assert.Equal(28.2724, row.LatitudeDeg);   // exactas, no las del texto DMS
		Assert.Equal(-16.6425, row.LongitudeDeg);
		Assert.Equal(2150, row.AltitudeMeters);
		Assert.Equal("28°16'21\" N", row.Latitude);
		Assert.False(string.IsNullOrEmpty(row.FechaActual));
		Assert.True(row.DateTicks > 0);
	}

	[Fact]
	public void PlanFavoriteSync_ImportedEntryNearby_MarksItInsteadOfInserting()
	{
		var imported = Imported("Casa", 40 + 10 * MeterLat, -3);
		var casa = Favorite("Mi casa", 40, -3, 650);

		var plan = LocationEntrySync.PlanFavoriteSync(new[] { imported }, new[] { casa }, Madrid);

		Assert.Empty(plan.ToInsert);
		var row = Assert.Single(plan.ToUpdate);
		Assert.Equal(imported.Id, row.Id);
		Assert.True(row.IsFavorite);
		Assert.Equal(casa.Id, row.FavoriteId);
		Assert.Equal(LocationEntrySources.Import, row.Source);
		Assert.Equal("Casa", row.Location);   // el nombre del registro importado no se toca
		Assert.Equal(650, row.AltitudeMeters);
		Assert.False(imported.IsFavorite);    // no modifica la lista original
	}

	[Fact]
	public void PlanFavoriteSync_AlreadySynced_DoesNothing()
	{
		var casa = Favorite("Mi casa", 40, -3, 650);
		var first = LocationEntrySync.PlanFavoriteSync(Array.Empty<LocationEntry>(), new[] { casa }, Madrid);

		var second = LocationEntrySync.PlanFavoriteSync(first.ToInsert, new[] { casa }, Madrid);

		Assert.True(second.IsEmpty);
	}

	[Fact]
	public void PlanFavoriteSync_RenamedOrMovedFavorite_UpdatesItsRow()
	{
		var casa = Favorite("Mi casa", 40, -3, 650);
		var existing = LocationEntrySync.PlanFavoriteSync(Array.Empty<LocationEntry>(), new[] { casa }, Madrid).ToInsert;
		var originalTicks = existing[0].DateTicks;

		casa.Name = "Casa de campo";
		casa.Latitude = 41;
		var plan = LocationEntrySync.PlanFavoriteSync(existing, new[] { casa }, Madrid);

		Assert.Empty(plan.ToInsert);
		var row = Assert.Single(plan.ToUpdate);
		Assert.Equal("Casa de campo", row.Location);
		Assert.Equal(41, row.LatitudeDeg);
		Assert.Equal("41°0'0\" N", row.Latitude);
		Assert.Equal(originalTicks, row.DateTicks);
	}

	[Fact]
	public void PlanFavoriteSync_FavoriteMovedAwayFromMarkedImport_UnmarksItAndFindsNewPlace()
	{
		var imported = Imported("Casa", 40, -3);
		var casa = Favorite("Mi casa", 40, -3);
		var marked = LocationEntrySync.PlanFavoriteSync(new[] { imported }, new[] { casa }, Madrid).ToUpdate;

		casa.Latitude = 41;
		var plan = LocationEntrySync.PlanFavoriteSync(marked, new[] { casa }, Madrid);

		var unmarked = Assert.Single(plan.ToUpdate);
		Assert.False(unmarked.IsFavorite);
		Assert.Null(unmarked.FavoriteId);
		var inserted = Assert.Single(plan.ToInsert);
		Assert.Equal(casa.Id, inserted.FavoriteId);
	}

	[Fact]
	public void PlanFavoriteSync_TwoFavoritesSamePlace_DoNotShareRow()
	{
		var a = Favorite("A", 40, -3);
		var b = Favorite("B", 40 + 5 * MeterLat, -3);

		var plan = LocationEntrySync.PlanFavoriteSync(Array.Empty<LocationEntry>(), new[] { a, b }, Madrid);

		Assert.Equal(2, plan.ToInsert.Count);
		Assert.Equal(new[] { a.Id, b.Id }, plan.ToInsert.Select(e => e.FavoriteId!.Value));
	}

	[Fact]
	public void PlanFavoriteRemoval_DeletesOwnRowAndUnmarksImports()
	{
		var favoriteId = Guid.NewGuid();
		var own = Imported("Propia", 40, -3);
		own.Source = LocationEntrySources.Favorite;
		own.IsFavorite = true;
		own.FavoriteId = favoriteId;
		var marked = Imported("Importada", 40, -3);
		marked.IsFavorite = true;
		marked.FavoriteId = favoriteId;
		var other = Imported("Otra", 40, -3);

		var plan = LocationEntrySync.PlanFavoriteRemoval(new[] { own, marked, other }, favoriteId);

		Assert.Equal(new[] { own.Id }, plan.ToDelete);
		var row = Assert.Single(plan.ToUpdate);
		Assert.Equal(marked.Id, row.Id);
		Assert.False(row.IsFavorite);
		Assert.Null(row.FavoriteId);
		Assert.Empty(plan.ToInsert);
	}

	[Fact]
	public void CreateEntry_Planner_HasSourceAndExactCoordinates()
	{
		var entry = LocationEntryMapper.CreateEntry("Observatorio", 40.123456, -3.654321, null,
			new DateTime(2026, 11, 24, 20, 0, 0, DateTimeKind.Utc), Madrid, LocationEntrySources.Planner);

		Assert.Equal(LocationEntrySources.Planner, entry.Source);
		Assert.False(entry.IsFavorite);
		Assert.Equal(40.123456, entry.LatitudeDeg);
		Assert.Equal(-3.654321, entry.LongitudeDeg);
		Assert.Equal("21:00:00", entry.HoraLocal);   // CET = UTC+1 en noviembre
		Assert.Equal("+01:00", entry.Desfase);
	}

	[Fact]
	public void BuildPickerOptions_FavoritesFirstByName_ThenNewestOthers()
	{
		var favB = Imported("Bravo", 41, -3);
		favB.IsFavorite = true;
		var favA = Imported("Alfa", 42, -3);
		favA.IsFavorite = true;
		var old = Imported("Antigua", 43, -3, ticks: 100);
		var recent = Imported("Reciente", 44, -3, ticks: 200);

		var names = LocationEntrySync.BuildPickerOptions(new[] { old, favB, recent, favA }).Select(o => o.Name);

		Assert.Equal(new[] { "Alfa", "Bravo", "Reciente", "Antigua" }, names);
	}

	[Fact]
	public void BuildPickerOptions_CollapsesNearbyNonFavorites()
	{
		var fav = Imported("Casa", 40, -3);
		fav.IsFavorite = true;
		var visit1 = Imported("Casa (visita)", 40 + 10 * MeterLat, -3, ticks: 300);
		var visit2 = Imported("Campo", 41, -3, ticks: 200);
		var visit3 = Imported("Campo otra vez", 41 + 3 * MeterLat, -3, ticks: 100);

		var names = LocationEntrySync.BuildPickerOptions(new[] { fav, visit1, visit2, visit3 }).Select(o => o.Name);

		Assert.Equal(new[] { "Casa", "Campo" }, names);
	}

	[Fact]
	public void BuildPickerOptions_KeepsNearbyFavorites()
	{
		var a = Imported("A", 40, -3);
		a.IsFavorite = true;
		var b = Imported("B", 40 + 5 * MeterLat, -3);
		b.IsFavorite = true;

		Assert.Equal(2, LocationEntrySync.BuildPickerOptions(new[] { a, b }).Count);
	}

	[Fact]
	public void BuildPickerOptions_AddsLocalFavoritesNotYetSynced()
	{
		var synced = Favorite("Sincronizada", 40, -3);
		var row = LocationEntrySync.PlanFavoriteSync(Array.Empty<LocationEntry>(), new[] { synced }, Madrid).ToInsert[0];
		var pending = Favorite("Pendiente", 42, -3, 900);

		var options = LocationEntrySync.BuildPickerOptions(new[] { row }, new[] { synced, pending });

		Assert.Equal(new[] { "Pendiente", "Sincronizada" }, options.Select(o => o.Name));
		Assert.All(options, o => Assert.True(o.IsFavorite));
		Assert.Equal(900, options[0].AltitudeMeters);
	}

	[Fact]
	public void BuildPickerOptions_SkipsEntriesWithoutCoordinates()
	{
		Assert.Empty(LocationEntrySync.BuildPickerOptions(new[] { new LocationEntry { Location = "X" } }));
	}
}
