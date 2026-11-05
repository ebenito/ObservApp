using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Calculadoras;

public sealed class SolLunaCalculatorServiceTests
{
	private readonly SolLunaCalculatorService _service = new();

	private const double MadridLat = 40.4168;
	private const double MadridLon = -3.7038;
	private const double MadridAltM = 650.0;

	private static readonly DateTime SummerSolstice2026 = new(2026, 6, 21);

	private SolLunaResult Madrid(DateTime date) => _service.Calculate(date, MadridLat, MadridLon, MadridAltM);

	private static void AssertNear(DateTime expectedUtc, DateTime? actualUtc, double toleranceMinutes)
	{
		Assert.NotNull(actualUtc);
		var diff = Math.Abs((actualUtc!.Value - expectedUtc).TotalMinutes);
		Assert.True(diff <= toleranceMinutes, $"Esperado {expectedUtc:u}, obtenido {actualUtc:u} (Δ {diff:F1} min)");
	}

	// ====================================================================
	// Sol
	// ====================================================================

	[Fact]
	public void Sun_SummerSolsticeMadrid_RiseAndSetMatchReference()
	{
		var sun = Madrid(SummerSolstice2026).Sun;

		// Referencia (IGN / timeanddate): orto 06:44 CEST, ocaso 21:48 CEST
		AssertNear(new DateTime(2026, 6, 21, 4, 44, 0, DateTimeKind.Utc), sun.Sunrise, 3);
		AssertNear(new DateTime(2026, 6, 21, 19, 48, 0, DateTimeKind.Utc), sun.Sunset, 3);
		Assert.InRange(sun.DayLength!.Value.TotalHours, 15.0, 15.2);
	}

	[Fact]
	public void Sun_SummerSolsticeMadrid_MaxAltitudeIsColatitudePlusObliquity()
	{
		var sun = Madrid(SummerSolstice2026).Sun;

		// 90 − 40,42 + 23,44 ≈ 73,0° (más ~0,005° de refracción)
		Assert.Equal(73.0, sun.MaxAltitudeDeg, 0);
		Assert.InRange(sun.DeclinationDeg, 23.40, 23.45);
		Assert.InRange(sun.DistanceAu, 1.015, 1.017); // cerca del afelio
	}

	[Fact]
	public void Sun_TwilightsAreOrdered()
	{
		var sun = Madrid(new DateTime(2026, 3, 15)).Sun;

		Assert.True(sun.AstronomicalDawn < sun.NauticalDawn);
		Assert.True(sun.NauticalDawn < sun.CivilDawn);
		Assert.True(sun.CivilDawn < sun.Sunrise);
		Assert.True(sun.Sunrise < sun.Transit);
		Assert.True(sun.Transit < sun.Sunset);
		Assert.True(sun.Sunset < sun.CivilDusk);
		Assert.True(sun.CivilDusk < sun.NauticalDusk);
		Assert.True(sun.NauticalDusk < sun.AstronomicalDusk);
	}

	[Fact]
	public void Sun_RiseAzimuthIsNortheastInSummer()
	{
		var sun = Madrid(SummerSolstice2026).Sun;

		Assert.InRange(sun.RiseAzimuthDeg, 55, 62);
		Assert.InRange(sun.SetAzimuthDeg, 298, 305);
	}

	[Theory]
	[InlineData(2026, 11, 3, 16.4)]   // máximo positivo del año
	[InlineData(2026, 2, 11, -14.2)]  // mínimo del año
	[InlineData(2026, 4, 15, 0.0)]    // cruce por cero
	public void Sun_EquationOfTime_MatchesReference(int y, int m, int d, double expectedMinutes)
	{
		var sun = Madrid(new DateTime(y, m, d)).Sun;

		Assert.InRange(sun.EquationOfTimeMinutes, expectedMinutes - 0.5, expectedMinutes + 0.5);
	}

	[Fact]
	public void Sun_EquationOfTime_StaysWithinPhysicalBoundsAllYear()
	{
		for (var date = new DateTime(2026, 1, 1); date.Year == 2026; date = date.AddDays(7))
		{
			var eot = Madrid(date).Sun.EquationOfTimeMinutes;
			Assert.InRange(eot, -14.6, 16.6);
		}
	}

	[Fact]
	public void Sun_PathHas24HourlyPointsPeakingAroundNoonUtc()
	{
		var path = Madrid(SummerSolstice2026).Sun.Path;

		Assert.Equal(24, path.Count);
		Assert.Equal(Enumerable.Range(0, 24), path.Select(p => p.Hour));
		var peak = path.MaxBy(p => p.AltitudeDeg)!;
		Assert.InRange(peak.Hour, 11, 13); // tránsito en Madrid ≈ 12:15 UTC
	}

	// ====================================================================
	// Luna
	// ====================================================================

	[Fact]
	public void Moon_OnFullMoonDay_IsFullAndFullyLit()
	{
		// Luna llena: 3-ene-2026 10:03 UTC
		var moon = Madrid(new DateTime(2026, 1, 3)).Moon;

		Assert.Equal(MoonPhaseKind.Full, moon.Phase);
		Assert.True(moon.IlluminationPercent > 99, $"Iluminación {moon.IlluminationPercent:F1}%");
		Assert.InRange(moon.AgeDays, 14.0, 15.5);
	}

	[Fact]
	public void Moon_DistanceAndAngularDiameterAreConsistent()
	{
		var moon = Madrid(new DateTime(2026, 1, 3)).Moon;

		Assert.InRange(moon.DistanceKm, 356_000, 407_000);
		Assert.InRange(moon.AngularDiameterDeg, 0.48, 0.57);
	}

	[Fact]
	public void Moon_NextNewAndFullMoonAreWithinOneLunation()
	{
		var date = new DateTime(2026, 1, 3);
		var moon = Madrid(date).Moon;

		// Luna nueva: 18-ene-2026 19:52 UTC
		AssertNear(new DateTime(2026, 1, 18, 19, 52, 0, DateTimeKind.Utc), moon.NextNewMoonUtc, 10);
		Assert.True(moon.NextFullMoonUtc > date);
		Assert.True(moon.NextFullMoonUtc < date.AddDays(30));
	}

	[Theory]
	[InlineData(0, MoonPhaseKind.New)]
	[InlineData(45, MoonPhaseKind.WaxingCrescent)]
	[InlineData(90, MoonPhaseKind.FirstQuarter)]
	[InlineData(135, MoonPhaseKind.WaxingGibbous)]
	[InlineData(180, MoonPhaseKind.Full)]
	[InlineData(225, MoonPhaseKind.WaningGibbous)]
	[InlineData(270, MoonPhaseKind.LastQuarter)]
	[InlineData(315, MoonPhaseKind.WaningCrescent)]
	[InlineData(350, MoonPhaseKind.New)]
	public void ClassifyPhase_MapsAngleToPhase(double angle, MoonPhaseKind expected)
	{
		Assert.Equal(expected, _service.ClassifyPhase(angle));
	}

	// ====================================================================
	// Eventos del año
	// ====================================================================

	[Fact]
	public void Events_2026SeasonsMatchReference()
	{
		var events = Madrid(SummerSolstice2026).Events;

		AssertNear(new DateTime(2026, 3, 20, 14, 46, 0, DateTimeKind.Utc), events.MarchEquinoxUtc, 5);
		AssertNear(new DateTime(2026, 6, 21, 8, 24, 0, DateTimeKind.Utc), events.JuneSolsticeUtc, 5);
		AssertNear(new DateTime(2026, 9, 23, 0, 5, 0, DateTimeKind.Utc), events.SeptemberEquinoxUtc, 5);
		AssertNear(new DateTime(2026, 12, 21, 20, 50, 0, DateTimeKind.Utc), events.DecemberSolsticeUtc, 5);
	}

	[Fact]
	public void Events_2026Has13FullMoons_IncludingBlueMoonInMay()
	{
		var phases = Madrid(SummerSolstice2026).Events.MoonPhases;
		var fullMoons = phases.Where(p => p.Phase == MoonPhaseKind.Full).ToList();

		Assert.Equal(13, fullMoons.Count);
		Assert.Equal(2, fullMoons.Count(p => p.UtcTime.Month == 5));
		Assert.All(phases, p => Assert.Equal(2026, p.UtcTime.Year));
		Assert.Equal(phases.OrderBy(p => p.UtcTime), phases);
	}

	[Fact]
	public void Events_TotalSolarEclipse2026_IsTotalFromLeon()
	{
		// León está dentro de la franja de totalidad del 12-ago-2026
		var events = _service.Calculate(SummerSolstice2026, 42.5987, -5.5671, 840).Events;

		var eclipse = Assert.Single(events.Eclipses, e => e.Body == SolLunaEclipseBody.Solar && e.PeakUtc.Month == 8);
		Assert.Equal(SolLunaEclipseKind.Total, eclipse.Kind);
		Assert.Equal(12, eclipse.PeakUtc.Day);
		Assert.NotNull(eclipse.C2Utc);
		Assert.NotNull(eclipse.C3Utc);
		Assert.True(eclipse.C1Utc < eclipse.C2Utc && eclipse.C3Utc < eclipse.C4Utc);
	}

	[Fact]
	public void Events_FromMadrid_ExcludesEclipsesWithMoonOrSunBelowHorizon()
	{
		var eclipses = Madrid(SummerSolstice2026).Events.Eclipses;

		// 17-feb (anular en la Antártida) y 3-mar (lunar total a las 11:33 UTC, Luna bajo el horizonte)
		Assert.DoesNotContain(eclipses, e => e.PeakUtc.Month is 2 or 3);
	}

	[Fact]
	public void Events_PartialLunarEclipseAug2026_IsVisibleFromMadridWithUmbralObscuration()
	{
		var eclipses = Madrid(SummerSolstice2026).Events.Eclipses;

		var lunar = Assert.Single(eclipses, e => e.Body == SolLunaEclipseBody.Lunar && e.PeakUtc.Month == 8);
		Assert.Equal(SolLunaEclipseKind.Partial, lunar.Kind);
		Assert.Equal(28, lunar.PeakUtc.Day);
		// Fracción del disco cubierta por la umbra (magnitud umbral 0,93), nunca una semiduración en minutos
		Assert.InRange(lunar.Obscuration, 0.5, 1.0);
	}
}
