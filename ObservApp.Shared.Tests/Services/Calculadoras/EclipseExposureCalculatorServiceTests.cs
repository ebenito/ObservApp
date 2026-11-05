using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Calculadoras;

public sealed class EclipseExposureCalculatorServiceTests
{
	private readonly EclipseExposureCalculatorService _service = new();

	// Valores por defecto de la página: 500 mm, f/8, ISO 100, Full Frame
	private static readonly EclipseExposureInput DefaultInput = new(500, 8, 100, 36, 24);

	[Fact]
	public void Calculate_ComputesFocalPlaneOptics()
	{
		var result = _service.Calculate(DefaultInput);

		Assert.Equal(4.587, result.SolarImageDiameterMm, 3);        // 500 / 109
		Assert.Equal(12.74, result.SensorWidthCoveragePercent, 2);
		Assert.Equal(4.955, result.DiagonalFieldDeg, 3);             // 2·atan(43,27 / 1000)
	}

	[Fact]
	public void Calculate_ReturnsAllNasaPhasesInOrder()
	{
		var result = _service.Calculate(DefaultInput);

		Assert.Equal(11, result.Rows.Count);
		Assert.Equal(new[] { -6, -4, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, result.Rows.Select(r => r.Q));
		Assert.All(result.Rows.Where(r => r.IsPartial), r => Assert.False(r.IsTotality));
		Assert.False(result.Rows.Single(r => r.Phase == EclipsePhotoPhase.BailysBeads).IsTotality);
	}

	[Fact]
	public void Calculate_AppliesNasaFormula()
	{
		var corona = _service.Calculate(DefaultInput).Rows.Single(r => r.Phase == EclipsePhotoPhase.Corona10);

		// t = f² / (ISO · 2^Q) = 64 / (100 · 128) = 1/200 s
		Assert.Equal(0.005, corona.ExactSeconds, 9);
		Assert.Equal(1.0 / 250, corona.SuggestedSeconds, 9);
	}

	[Fact]
	public void Calculate_LongExposure_SuggestsLongestStandardSpeed()
	{
		var partial = _service.Calculate(DefaultInput).Rows.Single(r => r.Phase == EclipsePhotoPhase.PartialOd50);

		// 64 / (100 · 2^-6) = 40,96 s
		Assert.Equal(40.96, partial.ExactSeconds, 6);
		Assert.Equal(2.0, partial.SuggestedSeconds, 9);
	}

	[Fact]
	public void Calculate_DoublingIso_HalvesExposure()
	{
		var at100 = _service.Calculate(DefaultInput).Rows;
		var at200 = _service.Calculate(DefaultInput with { Iso = 200 }).Rows;

		for (var i = 0; i < at100.Count; i++)
		{
			Assert.Equal(at100[i].ExactSeconds / 2, at200[i].ExactSeconds, 12);
		}
	}

	[Fact]
	public void Calculate_EachQStep_IsOneStop()
	{
		var rows = _service.Calculate(DefaultInput).Rows;
		var q5 = rows.Single(r => r.Q == 5).ExactSeconds;
		var q6 = rows.Single(r => r.Q == 6).ExactSeconds;

		Assert.Equal(2.0, q5 / q6, 12);
	}

	[Theory]
	[InlineData(0, 8, 100)]
	[InlineData(500, 0, 100)]
	[InlineData(500, 8, 0)]
	public void Calculate_WithNonPositiveInput_Throws(double focal, double fNumber, int iso)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => _service.Calculate(new EclipseExposureInput(focal, fNumber, iso, 36, 24)));
	}
}
