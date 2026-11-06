using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Calculadoras;

public sealed class FovCalculatorServiceTests
{
	private readonly FovCalculatorService _service = new();

	[Fact]
	public void Calculate_WithDefaultPageValues_ReturnsExpectedValues()
	{
		// Valores por defecto de la página: 900 mm, 130 mm, ocular 25 mm de 52°
		var result = _service.Calculate(new FovCalculationInput(900, 130, 25, 52));

		Assert.Equal(36, result.Magnification, 6);
		Assert.Equal(1.4444, result.TrueFieldDeg, 4);
		Assert.Equal(86.67, result.TrueFieldArcmin, 2);
		Assert.Equal(3.611, result.ExitPupilMm, 3);
		Assert.Equal(6.923, result.FocalRatio, 3);
		Assert.False(result.IsNearMaxMagnification);
		Assert.Equal(FovExitPupilQuality.Planetary, result.ExitPupilQuality);
		Assert.Equal(FovReferenceSize.SmallerThanPleiades, result.ReferenceSize);
	}

	[Fact]
	public void Calculate_ExitPupilEqualsEyepieceFocalOverFocalRatio()
	{
		var result = _service.Calculate(new FovCalculationInput(1200, 200, 10, 68));

		// EP = D / M = Fe / (f/)
		Assert.Equal(10 / result.FocalRatio, result.ExitPupilMm, 9);
	}

	[Fact]
	public void Calculate_AboveNinetyPercentOfMaxUseful_FlagsNearMaxMagnification()
	{
		// M = 400 > 130 × 1.8 = 234
		var result = _service.Calculate(new FovCalculationInput(2000, 130, 5, 52));

		Assert.True(result.IsNearMaxMagnification);
		Assert.Equal(FovExitPupilQuality.Narrow, result.ExitPupilQuality);
	}

	[Theory]
	[InlineData(400, 40, FovExitPupilQuality.AboveEyeLimit)] // EP 10 mm
	[InlineData(500, 30, FovExitPupilQuality.Good)]          // EP 6 mm
	[InlineData(1000, 20, FovExitPupilQuality.Planetary)]    // EP 2 mm (límite inferior)
	[InlineData(1000, 15, FovExitPupilQuality.Narrow)]       // EP 1,5 mm
	public void Calculate_ClassifiesExitPupil(double telescopeFocal, double eyepieceFocal, FovExitPupilQuality expected)
	{
		var result = _service.Calculate(new FovCalculationInput(telescopeFocal, 100, eyepieceFocal, 52));

		Assert.Equal(expected, result.ExitPupilQuality);
	}

	[Theory]
	[InlineData(100, FovReferenceSize.SmallerThanFullMoon)]   // 0,52°
	[InlineData(52, FovReferenceSize.AboutFullMoon)]          // 1,0°
	[InlineData(26, FovReferenceSize.SmallerThanPleiades)]    // 2,0°
	[InlineData(19, FovReferenceSize.AboutPleiades)]          // 2,74°
	[InlineData(10, FovReferenceSize.LargerThan3Deg)]         // 5,2°
	public void Calculate_ClassifiesTrueFieldAgainstReferences(double magnificationTarget, FovReferenceSize expected)
	{
		// AFOV 52° con M = Ft / Fe = magnificationTarget
		var result = _service.Calculate(new FovCalculationInput(magnificationTarget * 10, 100, 10, 52));

		Assert.Equal(expected, result.ReferenceSize);
	}

	[Theory]
	[InlineData(0, 130, 25, 52)]
	[InlineData(900, 0, 25, 52)]
	[InlineData(900, 130, 0, 52)]
	[InlineData(900, 130, 25, 0)]
	public void Calculate_WithNonPositiveInput_Throws(double ft, double d, double fe, double afov)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => _service.Calculate(new FovCalculationInput(ft, d, fe, afov)));
	}
}
