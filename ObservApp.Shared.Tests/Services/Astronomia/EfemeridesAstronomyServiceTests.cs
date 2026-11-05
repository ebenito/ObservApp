using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Astronomia;

public sealed class EfemeridesAstronomyServiceTests
{
	// ── Servicio bajo prueba ─────────────────────────────────────────────
	private static readonly EfemeridesAstronomyService Sut = new(
		new StubEclipseCalculatorService(),
		new StubStringLocalizer());

	// ── Constantes de ubicación y fecha ──────────────────────────────────
	private const double MadridLat = 40.4168;
	private const double MadridLon = -3.7038;
	private const double MadridAltM = 600.0;

	/// Fecha de referencia: noche de invierno en Madrid (DSO y planetas visibles)
	private static readonly DateTime WinterDate = new(2025, 1, 15);

	// ====================================================================
	// Compute — planetas
	// ====================================================================

	[Fact]
	public void Compute_ReturnsSeven_Planets()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		Assert.Equal(7, result.Planets.Count);
	}

	[Fact]
	public void Compute_PlanetsContainKnownBodies()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);
		var names = result.Planets.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

		Assert.Contains("Mercurio", names);
		Assert.Contains("Júpiter", names);
		Assert.Contains("Saturno", names);
		Assert.Contains("Neptuno", names);
	}

	[Fact]
	public void Compute_PlanetsMaxAltitude_WithinPhysicalRange()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		foreach (var planet in result.Planets)
		{
			Assert.True(
				planet.MaxAltitude is >= -90.0 and <= 90.0,
				$"{planet.Name}: altitud {planet.MaxAltitude:F1}° fuera de [-90, 90].");
		}
	}

	[Fact]
	public void Compute_PlanetsDistanceAU_NonNegative()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		foreach (var planet in result.Planets)
		{
			Assert.True(
				planet.DistanceAU >= 0.0,
				$"{planet.Name}: distancia {planet.DistanceAU} AU es negativa.");
		}
	}

	[Fact]
	public void Compute_PlanetsIllumination_WithinRange()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		foreach (var planet in result.Planets)
		{
			Assert.InRange(planet.IlluminationPct, 0.0, 100.0);
		}
	}

	// ====================================================================
	// Compute — tonight (luna)
	// ====================================================================

	[Fact]
	public void Compute_Tonight_IsNotNull_ForTemperateLocation()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		Assert.NotNull(result.Tonight);
	}

	[Fact]
	public void Compute_Tonight_MoonIllumination_WithinRange()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		Assert.NotNull(result.Tonight);
		Assert.InRange(result.Tonight.MoonIlluminationPct, 0.0, 100.0);
	}

	[Fact]
	public void Compute_Tonight_MoonPhaseAngle_WithinRange()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		Assert.NotNull(result.Tonight);
		Assert.InRange(result.Tonight.MoonPhaseAngle, 0.0, 360.0);
	}

	[Fact]
	public void Compute_Tonight_Quality_MatchesMoonIlluminationThresholds()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);
		Assert.NotNull(result.Tonight);

		var illum = result.Tonight.MoonIlluminationPct;
		var expectedQuality = illum < 25.0 ? EphNightQuality.Good
			: illum < 65.0 ? EphNightQuality.Fair
			: EphNightQuality.Poor;

		Assert.Equal(expectedQuality, result.Tonight.Quality);
	}

	// ====================================================================
	// Compute — eventos
	// ====================================================================

	[Fact]
	public void Compute_Events_AreOrderedByDate()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);

		var dates = result.Events.Select(e => e.Date).ToList();
		for (var i = 1; i < dates.Count; i++)
		{
			Assert.True(
				dates[i] >= dates[i - 1],
				$"Evento en posición {i} ({dates[i]:d}) anterior al anterior ({dates[i - 1]:d}).");
		}
	}

	[Fact]
	public void Compute_Events_DatesWithin90DaysWindow()
	{
		var result = Sut.Compute(WinterDate, MadridLat, MadridLon, MadridAltM);
		var limit = WinterDate.AddDays(91);

		foreach (var ev in result.Events)
		{
			Assert.True(
				ev.Date >= WinterDate.Date && ev.Date <= limit,
				$"Evento '{ev.Description}' el {ev.Date:d} fuera de la ventana de 90 días.");
		}
	}

	// ====================================================================
	// ComputeDsoVisibility
	// ====================================================================

	[Fact]
	public void ComputeDsoVisibility_ArcticSummer_ReturnsNoNightWindow()
	{
		// Svalbard (78°N) en solsticio de verano → sol de medianoche, sin noche astronómica
		var arcticSummer = new DateTime(2025, 6, 21);
		var orion = new EphDsoCoordinate { Id = "M42", RaDeg = 83.82, DecDeg = -5.39, Magnitude = 4.0 };

		var (window, visible) = Sut.ComputeDsoVisibility(
			arcticSummer, 78.0, 15.0, 0.0, [orion]);

		Assert.Null(window);
		Assert.Empty(visible);
	}

	[Fact]
	public void ComputeDsoVisibility_EmptyInput_ReturnsEmpty()
	{
		var (_, visible) = Sut.ComputeDsoVisibility(
			WinterDate, MadridLat, MadridLon, MadridAltM, []);

		Assert.Empty(visible);
	}

	[Fact]
	public void ComputeDsoVisibility_M42_VisibleInWinterFromMadrid()
	{
		// M42 (Nebulosa de Orión, RA=83.8°, Dec=−5.4°) es observable en enero desde Madrid
		var m42 = new EphDsoCoordinate { Id = "M42", RaDeg = 83.82, DecDeg = -5.39, Magnitude = 4.0 };

		var (window, visible) = Sut.ComputeDsoVisibility(
			WinterDate, MadridLat, MadridLon, MadridAltM, [m42]);

		// Si hay ventana nocturna en enero desde Madrid, M42 debe estar visible
		if (window is not null)
		{
			Assert.Contains(visible, v =>
				v.Id == "M42" &&
				v.IsVisibleTonight &&
				v.MaxAltitude > 10.0);
		}
	}

	[Fact]
	public void ComputeDsoVisibility_VisibleDso_HasValidPercentValues()
	{
		var m42 = new EphDsoCoordinate { Id = "M42", RaDeg = 83.82, DecDeg = -5.39, Magnitude = 4.0 };

		var (_, visible) = Sut.ComputeDsoVisibility(
			WinterDate, MadridLat, MadridLon, MadridAltM, [m42]);

		foreach (var dso in visible)
		{
			Assert.InRange(dso.VisibleStartPercent, 0.0, 100.0);
			Assert.InRange(dso.VisibleWidthPercent, 0.0, 100.0);
		}
	}

	// ====================================================================
	// FindNextVisibleEclipse
	// ====================================================================

	[Fact]
	public void FindNextVisibleEclipse_WithEmptyEclipseList_ReturnsBothNull()
	{
		// El stub devuelve lista vacía → no hay eclipse visible
		var (eclipse, result) = Sut.FindNextVisibleEclipse(
			DateTime.UtcNow, MadridLat, MadridLon, MadridAltM, yearsAhead: 2);

		Assert.Null(eclipse);
		Assert.Null(result);
	}

	// ====================================================================
	// Stubs de dependencias
	// ====================================================================

	private sealed class StubEclipseCalculatorService : IEclipseCalculatorService
	{
		public IReadOnlyList<EclipseDefinition> GetUpcomingEclipses(int years = 6)
			=> Array.Empty<EclipseDefinition>();

		public LocalEclipseResult? Calculate(
			EclipseDefinition eclipse,
			double latitudeDeg,
			double longitudeDeg,
			double altitudeMeters)
			=> null;
	}

	private sealed class StubStringLocalizer
		: IStringLocalizer<ObservApp.Resources.Strings.App>
	{
		public LocalizedString this[string name]
			=> new(name, name);

		public LocalizedString this[string name, params object[] arguments]
			=> new(name, string.Format(System.Globalization.CultureInfo.InvariantCulture, name, arguments));

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
			=> Enumerable.Empty<LocalizedString>();
	}
}
