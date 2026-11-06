namespace ObservApp.Shared.Services;

public sealed class FovCalculatorService : IFovCalculatorService
{
	// Aviso de aumento máximo a partir del 90 % del límite teórico (2 × D)
	private const double NearMaxMagnificationPerMm = 1.8;

	public FovCalculationResult Calculate(FovCalculationInput input)
	{
		ValidateInput(input);

		// M = Ft / Fe
		var magnification = input.TelescopeFocalLengthMm / input.EyepieceFocalLengthMm;

		// TFOV = AFOV / M
		var trueField = input.EyepieceApparentFieldDeg / magnification;

		// EP = D / M
		var exitPupil = input.ApertureMm / magnification;

		// f/ = Ft / D
		var focalRatio = input.TelescopeFocalLengthMm / input.ApertureMm;

		return new FovCalculationResult(
			Magnification: magnification,
			TrueFieldDeg: trueField,
			TrueFieldArcmin: trueField * 60d,
			ExitPupilMm: exitPupil,
			FocalRatio: focalRatio,
			IsNearMaxMagnification: magnification > input.ApertureMm * NearMaxMagnificationPerMm,
			ExitPupilQuality: ClassifyExitPupil(exitPupil),
			ReferenceSize: ClassifyTrueField(trueField));
	}

	private static FovExitPupilQuality ClassifyExitPupil(double exitPupilMm) => exitPupilMm switch
	{
		> 7d => FovExitPupilQuality.AboveEyeLimit,
		>= 5d => FovExitPupilQuality.Good,
		>= 2d => FovExitPupilQuality.Planetary,
		_ => FovExitPupilQuality.Narrow
	};

	private static FovReferenceSize ClassifyTrueField(double trueFieldDeg) => trueFieldDeg switch
	{
		< 0.6 => FovReferenceSize.SmallerThanFullMoon,
		< 1.1 => FovReferenceSize.AboutFullMoon,
		< 2.5 => FovReferenceSize.SmallerThanPleiades,
		< 3.0 => FovReferenceSize.AboutPleiades,
		_ => FovReferenceSize.LargerThan3Deg
	};

	private static void ValidateInput(FovCalculationInput input)
	{
		if (input.TelescopeFocalLengthMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.TelescopeFocalLengthMm), "Telescope focal length must be greater than zero.");
		}

		if (input.ApertureMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.ApertureMm), "Aperture must be greater than zero.");
		}

		if (input.EyepieceFocalLengthMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.EyepieceFocalLengthMm), "Eyepiece focal length must be greater than zero.");
		}

		if (input.EyepieceApparentFieldDeg <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.EyepieceApparentFieldDeg), "Eyepiece apparent field must be greater than zero.");
		}
	}
}
