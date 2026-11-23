using System.Text;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.UbicacionesRegistradas;

public sealed class LocationEntryMapperTests
{
	// 23-nov-2026 12:34:56.1234567, ticks que no caben en un double sin perder precisión.
	private const long SampleTicks = 639_000_000_001_234_567;

	private static LegacyLocationEntry Row(
		long ticks = SampleTicks,
		string location = "Olmeda de Cobeta",
		string lat = "40°51'33\" N",
		string lon = "2°11'48\" O") => new()
	{
		Id = 7,
		DateValue = ticks.ToString(),
		DateType = "integer",
		FechaActual = "23/11/2026",
		HoraLocal = "12:34:56",
		HoraUtc = "11:34:56",
		Desfase = "+01:00",
		TipoHorario = "Invierno",
		Location = location,
		Latitude = lat,
		Longitude = lon,
		Zona = 1
	};

	[Fact]
	public void FromLegacy_CopiesTextAndParsesCoordinates()
	{
		var entry = LocationEntryMapper.FromLegacy(Row());

		Assert.Equal(SampleTicks, entry.DateTicks);
		Assert.Equal("23/11/2026", entry.FechaActual);
		Assert.Equal("12:34:56", entry.HoraLocal);
		Assert.Equal("11:34:56", entry.HoraUtc);
		Assert.Equal("+01:00", entry.Desfase);
		Assert.Equal("Invierno", entry.TipoHorario);
		Assert.Equal("Olmeda de Cobeta", entry.Location);
		Assert.Equal("40°51'33\" N", entry.Latitude);
		Assert.Equal("2°11'48\" O", entry.Longitude);
		Assert.Equal(1, entry.Zona);
		Assert.True(entry.HasCoordinates());
		Assert.Equal(40.859167, entry.LatitudeDeg!.Value, 1e-6);
		Assert.Equal(-2.196667, entry.LongitudeDeg!.Value, 1e-6);
	}

	[Fact]
	public void FromLegacy_InvalidCoordinates_KeepsTextWithoutDecimals()
	{
		var entry = LocationEntryMapper.FromLegacy(Row(lat: "??", lon: "2°11'48\" O"));

		Assert.False(entry.HasCoordinates());
		Assert.Null(entry.LatitudeDeg);
		Assert.Null(entry.LongitudeDeg);
		Assert.Equal("??", entry.Latitude);
	}

	[Fact]
	public void FromLegacy_NullTexts_BecomeEmptyStrings()
	{
		var entry = LocationEntryMapper.FromLegacy(new LegacyLocationEntry { DateType = "null" });

		Assert.Equal(string.Empty, entry.Location);
		Assert.Equal(string.Empty, entry.HoraUtc);
		Assert.Equal(0, entry.DateTicks);
		Assert.False(entry.HasCoordinates());
	}

	[Fact]
	public void ToLegacy_RoundTripsExactly()
	{
		var original = Row();
		var back = LocationEntryMapper.ToLegacy(LocationEntryMapper.FromLegacy(original), 3);

		Assert.Equal(3, back.Id);
		Assert.Equal(original.DateValue, back.DateValue);
		Assert.Equal("integer", back.DateType);
		Assert.Equal(original.FechaActual, back.FechaActual);
		Assert.Equal(original.HoraLocal, back.HoraLocal);
		Assert.Equal(original.HoraUtc, back.HoraUtc);
		Assert.Equal(original.Desfase, back.Desfase);
		Assert.Equal(original.TipoHorario, back.TipoHorario);
		Assert.Equal(original.Location, back.Location);
		Assert.Equal(original.Latitude, back.Latitude);
		Assert.Equal(original.Longitude, back.Longitude);
		Assert.Equal(original.Zona, back.Zona);
	}

	[Fact]
	public void ToLegacyExport_OrdersOldestFirstAndNumbersFromOne()
	{
		var newer = LocationEntryMapper.FromLegacy(Row(ticks: SampleTicks + TimeSpan.TicksPerDay, location: "B"));
		var older = LocationEntryMapper.FromLegacy(Row(ticks: SampleTicks, location: "A"));

		var rows = LocationEntryMapper.ToLegacyExport(new[] { newer, older });

		Assert.Equal(new long[] { 1, 2 }, rows.Select(r => r.Id));
		Assert.Equal(new[] { "A", "B" }, rows.Select(r => r.Location));
	}

	[Theory]
	[InlineData("2026-11-23T12:34:56", "text")]
	[InlineData("2026-11-23 12:34:56", "text")]
	public void ParseDate_TextValues_AreParsed(string value, string type)
	{
		Assert.Equal(new DateTime(2026, 11, 23, 12, 34, 56), LocationEntryMapper.ParseDate(value, type));
	}

	[Theory]
	[InlineData(null, "null")]
	[InlineData("", "integer")]
	[InlineData("-5", "integer")]
	[InlineData("no es fecha", "text")]
	public void ParseDate_InvalidValues_ReturnMinValue(string? value, string type)
	{
		Assert.Equal(DateTime.MinValue, LocationEntryMapper.ParseDate(value, type));
	}

	[Fact]
	public void PlanImport_SkipsExistingAndRepeatedRows()
	{
		var existing = new List<LocationEntry> { LocationEntryMapper.FromLegacy(Row(location: "A")) };
		var incoming = new[]
		{
			Row(location: "A"),                                       // ya existe
			Row(ticks: SampleTicks + 999, location: " a "),           // mismo segundo y misma ubicación
			Row(ticks: SampleTicks + TimeSpan.TicksPerHour, location: "B"),
			Row(ticks: SampleTicks + TimeSpan.TicksPerHour, location: "B"), // repetida en el archivo
			Row(ticks: SampleTicks + 2 * TimeSpan.TicksPerHour, location: "C", lat: "x")
		};

		var plan = LocationEntryMapper.PlanImport(existing, incoming);

		Assert.Equal(new[] { "B", "C" }, plan.ToInsert.Select(e => e.Location));
		Assert.Equal(3, plan.Duplicates);
		Assert.Equal(1, plan.WithoutCoordinates);
	}

	[Fact]
	public void PlanImport_ReimportMonthsLater_OnlyAddsNewEntries()
	{
		// Primera importación: dos registros. Meses después el mismo archivo trae uno más,
		// y uno de los antiguos se ha renombrado en TiempoUbicacionApp.
		var firstImport = LocationEntryMapper.PlanImport(
			Array.Empty<LocationEntry>(),
			new[] { Row(location: "A"), Row(ticks: SampleTicks + TimeSpan.TicksPerDay, location: "B") });

		var secondImport = LocationEntryMapper.PlanImport(
			firstImport.ToInsert,
			new[]
			{
				Row(location: "A renombrado"),
				Row(ticks: SampleTicks + TimeSpan.TicksPerDay, location: "B"),
				Row(ticks: SampleTicks + 90 * TimeSpan.TicksPerDay, location: "Nueva")
			});

		Assert.Equal(new[] { "Nueva" }, secondImport.ToInsert.Select(e => e.Location));
		Assert.Equal(2, secondImport.Duplicates);
	}

	[Fact]
	public void PlanImport_SameCoordinatesWrittenDifferently_IsDuplicate()
	{
		var existing = new List<LocationEntry> { LocationEntryMapper.FromLegacy(Row(lat: "41°5'35\" N", lon: "3°27'55\" O")) };

		var plan = LocationEntryMapper.PlanImport(existing, new[] { Row(lat: "41º 5' 35'' N", lon: "3º 27' 55'' W") });

		Assert.Empty(plan.ToInsert);
		Assert.Equal(1, plan.Duplicates);
	}

	[Fact]
	public void PlanImport_SamePlaceDifferentSecond_IsNotDuplicate()
	{
		var plan = LocationEntryMapper.PlanImport(
			Array.Empty<LocationEntry>(),
			new[] { Row(), Row(ticks: SampleTicks + TimeSpan.TicksPerSecond) });

		Assert.Equal(2, plan.ToInsert.Count);
		Assert.Equal(0, plan.Duplicates);
	}

	[Fact]
	public void PlanImport_SameMomentDifferentPlace_IsNotDuplicate()
	{
		var plan = LocationEntryMapper.PlanImport(
			Array.Empty<LocationEntry>(),
			new[] { Row(location: "A"), Row(location: "A", lat: "41°00'00\" N") });

		Assert.Equal(2, plan.ToInsert.Count);
		Assert.Equal(0, plan.Duplicates);
	}

	[Fact]
	public void HasSqliteHeader_DetectsSqliteFiles()
	{
		var header = Encoding.ASCII.GetBytes("SQLite format 3\0").Concat(new byte[84]).ToArray();

		Assert.True(SqlJsLocationDbFileCodec.HasSqliteHeader(header));
		Assert.False(SqlJsLocationDbFileCodec.HasSqliteHeader(Encoding.ASCII.GetBytes("SQLite format 3")));
		Assert.False(SqlJsLocationDbFileCodec.HasSqliteHeader(Encoding.ASCII.GetBytes("PK\u0003\u0004 not a database")));
	}
}
