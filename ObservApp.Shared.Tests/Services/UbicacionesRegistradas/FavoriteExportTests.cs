using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.UbicacionesRegistradas;

public sealed class FavoriteExportTests
{
	private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

	private static FavoriteLocation Teide(DateTime createdUtc) => new()
	{
		Name = "Teide",
		Latitude = 28.2724,
		Longitude = -16.6425,
		AltitudeMeters = 2150,
		CreatedAt = createdUtc
	};

	[Theory]
	[InlineData(40.859167, true, "40°51'33\" N")]
	[InlineData(-2.196667, false, "2°11'48\" O")]
	[InlineData(-33.45, true, "33°27'0\" S")]
	[InlineData(3.703611, false, "3°42'13\" E")]
	[InlineData(40.99999, true, "41°0'0\" N")] // el redondeo de segundos arrastra minutos y grados
	public void FormatDms_UsesTiempoUbicacionAppFormat(double value, bool isLatitude, string expected)
	{
		Assert.Equal(expected, CoordinateParser.FormatDms(value, isLatitude));
	}

	[Theory]
	[InlineData(28.2724, true)]
	[InlineData(-16.6425, false)]
	public void FormatDms_RoundTripsWithinOneArcSecond(double value, bool isLatitude)
	{
		Assert.True(CoordinateParser.TryParse(CoordinateParser.FormatDms(value, isLatitude), out var back));
		Assert.Equal(value, back, 1.0 / 3600);
	}

	[Fact]
	public void FavoriteToLegacy_Summer_UsesLocalTimeAndSpanishTexts()
	{
		var row = LocationEntryMapper.FavoriteToLegacy(Teide(new DateTime(2026, 5, 2, 16, 2, 27, DateTimeKind.Utc)), 9, Madrid);

		Assert.Equal(9, row.Id);
		Assert.Equal(new DateTime(2026, 5, 2, 18, 2, 27).Ticks.ToString(), row.DateValue);
		Assert.Equal("integer", row.DateType);
		Assert.Equal("sábado, 02 mayo 2026", row.FechaActual);
		Assert.Equal("18:02:27", row.HoraLocal);
		Assert.Equal("16:02:27", row.HoraUtc);
		Assert.Equal("+02:00", row.Desfase);
		Assert.Equal("Horario de ahorro de luz (verano)", row.TipoHorario);
		Assert.Equal("Teide", row.Location);
		Assert.Equal("28°16'21\" N", row.Latitude);
		Assert.Equal("16°38'33\" O", row.Longitude);
		Assert.Equal(0, row.Zona);
	}

	[Fact]
	public void FavoriteToLegacy_Winter_UsesStandardTime()
	{
		var row = LocationEntryMapper.FavoriteToLegacy(Teide(new DateTime(2026, 1, 20, 21, 39, 14, DateTimeKind.Utc)), 1, Madrid);

		Assert.Equal("22:39:14", row.HoraLocal);
		Assert.Equal("+01:00", row.Desfase);
		Assert.Equal("Horario estándar (invierno)", row.TipoHorario);
	}

	[Fact]
	public void FavoriteToLegacy_UnspecifiedKind_IsTreatedAsUtc()
	{
		var row = LocationEntryMapper.FavoriteToLegacy(Teide(new DateTime(2026, 1, 20, 21, 39, 14, DateTimeKind.Unspecified)), 1, Madrid);

		Assert.Equal("22:39:14", row.HoraLocal);
	}

	[Fact]
	public void BuildExport_MergesSourcesByDateAndNumbersFromOne()
	{
		var entry = LocationEntryMapper.FromLegacy(new LegacyLocationEntry
		{
			DateValue = new DateTime(2026, 3, 1, 12, 0, 0).Ticks.ToString(),
			DateType = "integer",
			Location = "Registro",
			Latitude = "40°0'0\" N",
			Longitude = "3°0'0\" O"
		});
		var older = Teide(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
		older.Name = "Favorita antigua";
		var newer = Teide(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
		newer.Name = "Favorita nueva";

		var rows = LocationEntryMapper.BuildExport(new[] { entry }, new[] { newer, older }, Madrid);

		Assert.Equal(new[] { "Favorita antigua", "Registro", "Favorita nueva" }, rows.Select(r => r.Location));
		Assert.Equal(new long[] { 1, 2, 3 }, rows.Select(r => r.Id));
	}

	[Fact]
	public void ExportedFavorite_ReimportsWithSameCoordinates()
	{
		var favorite = Teide(new DateTime(2026, 5, 2, 16, 2, 27, DateTimeKind.Utc));

		var entry = LocationEntryMapper.FromLegacy(LocationEntryMapper.FavoriteToLegacy(favorite, 1, Madrid));

		Assert.True(entry.HasCoordinates());
		Assert.Equal(favorite.Latitude, entry.LatitudeDeg!.Value, 1.0 / 3600);
		Assert.Equal(favorite.Longitude, entry.LongitudeDeg!.Value, 1.0 / 3600);
	}
}
