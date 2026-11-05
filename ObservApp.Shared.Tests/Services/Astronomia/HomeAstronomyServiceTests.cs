using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Astronomia;

public sealed class HomeAstronomyServiceTests
{
	private readonly HomeAstronomyService _service = new();

	private const double MadridLat = 40.4168;
	private const double MadridLon = -3.7038;
	private const double MadridAltM = 650.0;

	private HomeAstronomySnapshot Snapshot(DateTime nowUtc)
		=> _service.CalculateSnapshot(MadridLat, MadridLon, MadridAltM, nowUtc)!;

	[Fact]
	public void CalculateSnapshot_AtNoon_IsDaytimeAndNotAstroNight()
	{
		var snapshot = Snapshot(new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc));

		Assert.True(snapshot.IsDaytime);
		Assert.False(snapshot.IsAstroNight);
	}

	[Fact]
	public void CalculateSnapshot_LateEvening_IsAstroNight()
	{
		// Fin del crepúsculo astronómico en Madrid el 21-jun ≈ 21:40 UTC
		var snapshot = Snapshot(new DateTime(2026, 6, 21, 23, 0, 0, DateTimeKind.Utc));

		Assert.False(snapshot.IsDaytime);
		Assert.True(snapshot.IsAstroNight);
	}

	[Fact]
	public void CalculateSnapshot_SunTimesMatchSolLunaCalculator()
	{
		var now = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
		var snapshot = Snapshot(now);
		var sun = new SolLunaCalculatorService().Calculate(now.Date, MadridLat, MadridLon, MadridAltM).Sun;

		Assert.Equal(sun.Sunrise, snapshot.Sunrise);
		Assert.Equal(sun.Sunset, snapshot.Sunset);
		Assert.Equal(sun.AstronomicalDusk, snapshot.AstroTwilightEnd);
		Assert.Equal(sun.DayLength, snapshot.DayLength);
	}

	[Fact]
	public void CalculateSnapshot_NextFullMoon_MatchesReference()
	{
		// Luna llena: 29-jun-2026 23:57 UTC
		var snapshot = Snapshot(new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc));

		Assert.NotNull(snapshot.NextFullMoonUtc);
		var diff = Math.Abs((snapshot.NextFullMoonUtc!.Value - new DateTime(2026, 6, 29, 23, 57, 0)).TotalMinutes);
		Assert.True(diff < 15, $"Δ {diff:F1} min");
	}

	[Fact]
	public void CalculateSnapshot_MoonValuesAreInRange()
	{
		var snapshot = Snapshot(new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc));

		Assert.InRange(snapshot.MoonPhaseAngle, 0, 360);
		Assert.InRange(snapshot.MoonIllumination, 0, 100);
		Assert.InRange(snapshot.MoonAge, 0, 29.6);
	}
}
