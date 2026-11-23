using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Mapas;

public sealed class MapLocationListFilterTests
{
	private static MapLocationItem Item(string name, DateTime? date, MapMarkerKind kind = MapMarkerKind.Entry) =>
		new(Guid.NewGuid(), kind, name, date, 40, -3, "40, -3");

	private static readonly List<MapLocationItem> Items =
	[
		Item("Castillejo de Mesleón (Castilla y León)", new DateTime(2025, 8, 4, 15, 12, 32)),
		Item("El Cardoso de la Sierra", new DateTime(2025, 8, 20, 22, 0, 0)),
		Item("Madrid", new DateTime(2026, 1, 20, 22, 39, 14)),
		Item("Estebanvela", new DateTime(2024, 8, 4, 10, 0, 0)),
		Item("Teide", new DateTime(2026, 5, 2, 18, 2, 27), MapMarkerKind.Favorite),
		Item("", null)
	];

	private static List<string> Names(MapLocationQuery query) =>
		MapLocationListFilter.Apply(Items, query).Select(i => i.Name).ToList();

	[Fact]
	public void DefaultQuery_SortsNewestFirstWithUndatedLast()
	{
		Assert.Equal(
			new[] { "Teide", "Madrid", "El Cardoso de la Sierra", "Castillejo de Mesleón (Castilla y León)", "Estebanvela", "" },
			Names(new MapLocationQuery()));
	}

	[Fact]
	public void DateAsc_SortsOldestFirstWithUndatedLast()
	{
		var names = Names(new MapLocationQuery(Sort: MapLocationSort.DateAsc));

		Assert.Equal("Estebanvela", names[0]);
		Assert.Equal("", names[^1]);
	}

	[Fact]
	public void NameSort_IgnoresCase()
	{
		var names = Names(new MapLocationQuery(Search: "e", Sort: MapLocationSort.NameAsc));

		Assert.Equal(new[] { "Castillejo de Mesleón (Castilla y León)", "El Cardoso de la Sierra", "Estebanvela", "Teide" }, names);
	}

	[Theory]
	[InlineData("leon")]       // sin tilde
	[InlineData("LEÓN")]       // mayúsculas con tilde
	[InlineData("  mesleón ")] // espacios alrededor
	public void Search_IgnoresCaseAndAccents(string search)
	{
		Assert.Equal(new[] { "Castillejo de Mesleón (Castilla y León)" }, Names(new MapLocationQuery(Search: search)));
	}

	[Fact]
	public void Year_FiltersWholeYear()
	{
		Assert.Equal(
			new[] { "El Cardoso de la Sierra", "Castillejo de Mesleón (Castilla y León)" },
			Names(new MapLocationQuery(Year: 2025)));
	}

	[Fact]
	public void Month_FiltersThatMonthInAnyYear()
	{
		Assert.Equal(
			new[] { "El Cardoso de la Sierra", "Castillejo de Mesleón (Castilla y León)", "Estebanvela" },
			Names(new MapLocationQuery(Month: 8)));
	}

	[Fact]
	public void YearMonthDay_FiltersExactDate()
	{
		Assert.Equal(
			new[] { "Castillejo de Mesleón (Castilla y León)" },
			Names(new MapLocationQuery(Year: 2025, Month: 8, Day: 4)));
	}

	[Fact]
	public void DayAndMonth_WithoutYear_MatchesEveryYear()
	{
		Assert.Equal(
			new[] { "Castillejo de Mesleón (Castilla y León)", "Estebanvela" },
			Names(new MapLocationQuery(Month: 8, Day: 4)));
	}

	[Fact]
	public void Source_FiltersByKind()
	{
		Assert.Equal(new[] { "Teide" }, Names(new MapLocationQuery(Source: MapLocationSource.Favorites)));
		Assert.Equal(5, Names(new MapLocationQuery(Source: MapLocationSource.Entries)).Count);
	}

	[Fact]
	public void FiltersCombine()
	{
		Assert.Empty(Names(new MapLocationQuery(Search: "teide", Source: MapLocationSource.Entries)));
		Assert.Equal(new[] { "Madrid" }, Names(new MapLocationQuery(Search: "mad", Year: 2026)));
	}

	[Fact]
	public void AvailableYears_AreDistinctAndDescending()
	{
		Assert.Equal(new[] { 2026, 2025, 2024 }, MapLocationListFilter.AvailableYears(Items));
	}

	[Fact]
	public void IsDefault_OnlyWhenNoFilterIsActive()
	{
		Assert.True(new MapLocationQuery(Sort: MapLocationSort.NameDesc).IsDefault);
		Assert.False(new MapLocationQuery(Month: 8).IsDefault);
		Assert.False(new MapLocationQuery(Search: "x").IsDefault);
		Assert.False(new MapLocationQuery(Source: MapLocationSource.Favorites).IsDefault);
	}
}
