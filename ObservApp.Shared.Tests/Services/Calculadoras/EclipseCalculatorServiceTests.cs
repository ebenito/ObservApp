using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Calculadoras;

public sealed class EclipseCalculatorServiceTests
{
	private static readonly EclipseCalculatorService Sut = new(new FixedTimeProvider(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)));

	private static EclipseDefinition Spain2026 => Sut.GetUpcomingEclipses().Single(e => e.Id == "2026-08-12");

	// ====================================================================
	// Catálogo
	// ====================================================================

	[Fact]
	public void GetUpcomingEclipses_FiltersByNowAndHorizon()
	{
		var sut = new EclipseCalculatorService(new FixedTimeProvider(new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc)));

		var ids = sut.GetUpcomingEclipses(years: 2).Select(e => e.Id).ToList();

		Assert.Equal(new[] { "2027-02-06", "2027-08-02", "2028-01-26", "2028-07-22" }, ids);
	}

	[Fact]
	public void GetUpcomingEclipses_KeepsEclipseOnItsOwnDay()
	{
		var sut = new EclipseCalculatorService(new FixedTimeProvider(new DateTime(2026, 8, 12, 20, 0, 0, DateTimeKind.Utc)));

		Assert.Equal("2026-08-12", sut.GetUpcomingEclipses().First().Id);
	}

	[Fact]
	public void GetUpcomingEclipses_IsSortedByDate()
	{
		var list = Sut.GetUpcomingEclipses(years: 10);

		Assert.Equal(list.OrderBy(e => e.Date), list);
	}

	// ====================================================================
	// Cálculo local — 12-ago-2026
	// ====================================================================

	[Fact]
	public void Calculate_FromLeon_IsVisibleTotalEclipseWithOrderedContacts()
	{
		var result = Sut.Calculate(Spain2026, 42.5987, -5.5671, 840)!;

		Assert.Equal(LocalEclipseType.Total, result.Type);
		Assert.True(result.IsVisible);
		Assert.Equal(1.0, result.MaxCoverage, 3);

		var kinds = result.Contacts.Select(c => c.Kind).ToList();
		Assert.Equal(new[]
		{
			EclipseEventKind.C1, EclipseEventKind.ShadowBands, EclipseEventKind.BailyBeadsStart,
			EclipseEventKind.DiamondRingStart, EclipseEventKind.C2, EclipseEventKind.Maximum,
			EclipseEventKind.C3, EclipseEventKind.DiamondRingEnd, EclipseEventKind.BailyBeadsEnd,
			EclipseEventKind.C4
		}, kinds);
		Assert.Equal(result.Contacts.OrderBy(c => c.UtcTime), result.Contacts);
	}

	[Fact]
	public void Calculate_FromLeon_ContactsMatchReference()
	{
		var result = Sut.Calculate(Spain2026, 42.5987, -5.5671, 840)!;
		var c2 = result.Contacts.Single(c => c.Kind == EclipseEventKind.C2).UtcTime;
		var max = result.Contacts.Single(c => c.Kind == EclipseEventKind.Maximum);

		// Totalidad en León hacia las 18:27 UTC (20:27 CEST), Sol bajo hacia el oeste
		Assert.InRange(c2, new DateTime(2026, 8, 12, 18, 24, 0), new DateTime(2026, 8, 12, 18, 31, 0));
		Assert.InRange(max.SunAltitudeDeg, 5, 15);
		Assert.InRange(max.SunAzimuthDeg, 270, 295);
		Assert.InRange(result.Metrics!.TotalityDuration!.Value.TotalSeconds, 30, 120);
	}

	[Fact]
	public void Calculate_GlobalMetricsMatchNasa()
	{
		var metrics = Sut.Calculate(Spain2026, 42.5987, -5.5671, 840)!.Metrics!;

		// NASA: magnitud 1,0386, gamma 0,8977, Saros 126
		Assert.InRange(metrics.Magnitude, 1.033, 1.044);
		Assert.InRange(metrics.Gamma, 0.88, 0.91);
		Assert.InRange(metrics.DeltaTSeconds, 65, 80);
		Assert.Equal(126, metrics.SarosNumber);
	}

	[Fact]
	public void Calculate_FromSevilla_IsPartialWithoutTotalityContacts()
	{
		var result = Sut.Calculate(Spain2026, 37.3891, -5.9845, 10)!;

		Assert.Equal(LocalEclipseType.Partial, result.Type);
		Assert.InRange(result.MaxCoverage, 0.8, 0.999);
		Assert.DoesNotContain(result.Contacts, c => c.Kind is EclipseEventKind.C2 or EclipseEventKind.C3);
		Assert.Null(result.Metrics!.TotalityDuration);
	}

	[Fact]
	public void Calculate_FromSydney_IsNotVisible()
	{
		var result = Sut.Calculate(Spain2026, -33.8688, 151.2093, 50)!;

		Assert.Equal(LocalEclipseType.None, result.Type);
		Assert.False(result.IsVisible);
	}

	private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
	}
}
