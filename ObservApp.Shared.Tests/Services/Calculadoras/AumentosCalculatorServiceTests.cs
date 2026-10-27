using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Calculadoras;

public sealed class AumentosCalculatorServiceTests
{
	private readonly AumentosCalculatorService _service = new();

	[Fact]
	public void Calculate_WithKnownCase_ReturnsExpectedValues()
	{
		var result = _service.Calculate(new AumentosCalculationInput(
			ApertureMm: 200,
			TelescopeFocalLengthMm: 1200,
			EyepieceFocalLengthMm: 25,
			EyepieceApparentFieldDeg: 52,
			MultiplierFactor: 1));

		Assert.Equal(1200, result.EffectiveFocalLengthMm, 6);
		Assert.Equal(48, result.Magnification, 6);
		Assert.Equal(6, result.EffectiveFocalRatio, 6);
		Assert.Equal(4.17, result.ExitPupilMm, 2);
		Assert.Equal(1.08, result.TrueFieldDeg, 2);
		Assert.Equal(0.58, result.DawesLimitArcsec, 2);
		Assert.Equal(28.57, result.MinUsefulMagnification, 2);
		Assert.Equal(400, result.MaxUsefulMagnification, 6);
		Assert.False(result.IsAboveMaximumUseful);
		Assert.False(result.IsBelowMinimumUseful);
	}

	[Fact]
	public void Calculate_WithFieldStop_UsesFieldStopMethod()
	{
		var result = _service.Calculate(new AumentosCalculationInput(
			ApertureMm: 150,
			TelescopeFocalLengthMm: 900,
			EyepieceFocalLengthMm: 20,
			EyepieceApparentFieldDeg: 60,
			MultiplierFactor: 1,
			EyepieceFieldStopMm: 27));

		Assert.Equal(TrueFieldMethod.FieldStop, result.TrueFieldMethod);
		Assert.Equal(1.72, result.TrueFieldDeg, 2);
	}

	[Fact]
	public void Calculate_WithReducerFactor_ComputesReducedMagnification()
	{
		var result = _service.Calculate(new AumentosCalculationInput(
			ApertureMm: 200,
			TelescopeFocalLengthMm: 1200,
			EyepieceFocalLengthMm: 25,
			EyepieceApparentFieldDeg: 52,
			MultiplierFactor: 0.63));

		Assert.Equal(30.24, result.Magnification, 2);
		Assert.Equal(6.61, result.ExitPupilMm, 2);
		Assert.Equal(ExitPupilBand.WideField, result.ExitPupilBand);
	}

	[Theory]
	[InlineData(0, 1200, 25, 52, 1)]
	[InlineData(200, 0, 25, 52, 1)]
	[InlineData(200, 1200, 0, 52, 1)]
	[InlineData(200, 1200, 25, 0, 1)]
	[InlineData(200, 1200, 25, 52, 0)]
	[InlineData(-200, 1200, 25, 52, 1)]
	public void Calculate_WithNonPositiveInputs_Throws(
		double aperture,
		double telescopeFocal,
		double eyepieceFocal,
		double afov,
		double factor)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.Calculate(new AumentosCalculationInput(
				aperture,
				telescopeFocal,
				eyepieceFocal,
				afov,
				factor)));
	}

	[Fact]
	public void Calculate_WithHugeFieldStop_FlagsAsOutOfTypicalRange()
	{
		var result = _service.Calculate(new AumentosCalculationInput(
			ApertureMm: 200,
			TelescopeFocalLengthMm: 1200,
			EyepieceFocalLengthMm: 25,
			EyepieceApparentFieldDeg: 52,
			MultiplierFactor: 1,
			EyepieceFieldStopMm: 70));

		Assert.True(result.IsFieldStopOutOfTypicalRange);
	}

	[Fact]
	public void Calculate_WithVeryHighMagnification_FlagsOverLimit()
	{
		var result = _service.Calculate(new AumentosCalculationInput(
			ApertureMm: 120,
			TelescopeFocalLengthMm: 1200,
			EyepieceFocalLengthMm: 2,
			EyepieceApparentFieldDeg: 52,
			MultiplierFactor: 1));

		Assert.True(result.IsAboveMaximumUseful);
		Assert.Equal(ExitPupilBand.Overmagnified, result.ExitPupilBand);
	}

	[Theory]
	[InlineData(6.67, 5.5)]
	[InlineData(5.0, 14.3)]
	[InlineData(2.0, 42.2)]
	[InlineData(1.0, 63.3)]
	[InlineData(0.7, 74.2)]
	[InlineData(0.5, 84.4)]
	[InlineData(0.32, 98.0)]
	[InlineData(0.3, 100.0)]
	[InlineData(10.0, 0.0)]
	[InlineData(0.1, 100.0)]
	public void ExitPupilScale_PupilToPosition_WithKnownValues_ReturnsExpectedPercent(
		double pupilMm,
		double expected)
	{
		var result = ExitPupilScale.PupilToPosition(pupilMm);
		Assert.InRange(result, expected - 0.5, expected + 0.5);
	}

	[Fact]
	public void ExitPupilScale_PupilToPosition_IsStrictlyMonotonicBetween032And667()
	{
		var pupils = new[] { 0.32, 0.4, 0.5, 0.7, 1.0, 2.0, 4.0, 5.0, 6.67 };
		var positions = pupils.Select(ExitPupilScale.PupilToPosition).ToArray();

		for (var i = 1; i < positions.Length; i++)
		{
			Assert.True(
				positions[i] < positions[i - 1],
				$"La posición no es monótona: p={pupils[i - 1]} -> {positions[i - 1]} y p={pupils[i]} -> {positions[i]}");
		}
	}
}
