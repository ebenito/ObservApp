namespace ObservApp.Shared.Services;

public sealed class AumentosCalculatorService : IAumentosCalculatorService
{
	private const double DegreesPerRadian = 57.2958;
	private const double DarkAdaptedEyePupilMm = 7d;
	private const double MaxTypicalFieldStopMm = 55d;

	public AumentosCalculationResult Calculate(AumentosCalculationInput input)
	{
		ValidateInput(input);

		// Distancia focal efectiva = F × k
		var effectiveFocalLength = input.TelescopeFocalLengthMm * input.MultiplierFactor;

		// Aumentos = (F × k) / fe
		var magnification = effectiveFocalLength / input.EyepieceFocalLengthMm;

		// Relación focal efectiva = (F × k) / D
		var effectiveFocalRatio = effectiveFocalLength / input.ApertureMm;

		// Pupila de salida = D / M (equivalente a fe / f/)
		var exitPupil = input.ApertureMm / magnification;

		var usesFieldStop = input.EyepieceFieldStopMm.HasValue;

		// Campo real = (field stop / F efectiva) × 57.2958 o AFOV / M (aproximación)
		var trueFieldDeg = usesFieldStop
			? (input.EyepieceFieldStopMm!.Value / effectiveFocalLength) * DegreesPerRadian
			: input.EyepieceApparentFieldDeg / magnification;

		// Límites teóricos de resolución
		var dawesLimitArcsec = 116d / input.ApertureMm;
		var rayleighLimitArcsec = 138d / input.ApertureMm;

		// Aumento mínimo útil (pupila 7mm) y máximo útil teórico (2×D)
		var minUsefulMagnification = input.ApertureMm / DarkAdaptedEyePupilMm;
		var maxUsefulMagnification = 2d * input.ApertureMm;

		// Aumentos de referencia para pupilas de salida estándar
		var magnificationAt5Mm = input.ApertureMm / 5d;
		var magnificationAt2Mm = input.ApertureMm / 2d;
		var magnificationAt1Mm = input.ApertureMm / 1d;
		var magnificationAt05Mm = input.ApertureMm / 0.5d;

		// Ganancia de luz frente a ojo desnudo
		var lightGainVsNakedEye = Math.Pow(input.ApertureMm / DarkAdaptedEyePupilMm, 2d);

		// Magnitud límite visual aproximada (D en mm -> cm)
		var limitingMagnitude = 7.7d + 5d * Math.Log10(input.ApertureMm / 10d);

		var band = ClassifyExitPupil(exitPupil);
		var isAboveMaximumUseful = magnification > maxUsefulMagnification;
		var isBelowMinimumUseful = magnification < minUsefulMagnification;
		var isFieldStopOutOfTypicalRange = usesFieldStop && input.EyepieceFieldStopMm!.Value > MaxTypicalFieldStopMm;

		return new AumentosCalculationResult(
			EffectiveFocalLengthMm: effectiveFocalLength,
			Magnification: magnification,
			EffectiveFocalRatio: effectiveFocalRatio,
			ExitPupilMm: exitPupil,
			TrueFieldDeg: trueFieldDeg,
			TrueFieldMethod: usesFieldStop ? TrueFieldMethod.FieldStop : TrueFieldMethod.AfovApproximation,
			DawesLimitArcsec: dawesLimitArcsec,
			RayleighLimitArcsec: rayleighLimitArcsec,
			MinUsefulMagnification: minUsefulMagnification,
			MaxUsefulMagnification: maxUsefulMagnification,
			RecommendedMagnificationMin: minUsefulMagnification,
			RecommendedMagnificationMax: maxUsefulMagnification,
			MagnificationAt5MmExitPupil: magnificationAt5Mm,
			MagnificationAt2MmExitPupil: magnificationAt2Mm,
			MagnificationAt1MmExitPupil: magnificationAt1Mm,
			MagnificationAt05MmExitPupil: magnificationAt05Mm,
			LightGainVsNakedEye: lightGainVsNakedEye,
			LimitingMagnitude: limitingMagnitude,
			ExitPupilBand: band,
			IsBelowMinimumUseful: isBelowMinimumUseful,
			IsAboveMaximumUseful: isAboveMaximumUseful,
			IsFieldStopOutOfTypicalRange: isFieldStopOutOfTypicalRange);
	}

	private static ExitPupilBand ClassifyExitPupil(double exitPupilMm)
	{
		if (exitPupilMm > 7d)
		{
			return ExitPupilBand.AboveEyeLimit;
		}

		if (exitPupilMm >= 4d)
		{
			return ExitPupilBand.WideField;
		}

		if (exitPupilMm >= 2d)
		{
			return ExitPupilBand.GeneralPurpose;
		}

		if (exitPupilMm >= 1d)
		{
			return ExitPupilBand.Planetary;
		}

		if (exitPupilMm >= 0.5d)
		{
			return ExitPupilBand.HighPower;
		}

		return ExitPupilBand.Overmagnified;
	}

	private static void ValidateInput(AumentosCalculationInput input)
	{
		if (input.ApertureMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.ApertureMm), "Aperture must be greater than zero.");
		}

		if (input.TelescopeFocalLengthMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.TelescopeFocalLengthMm), "Telescope focal length must be greater than zero.");
		}

		if (input.EyepieceFocalLengthMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.EyepieceFocalLengthMm), "Eyepiece focal length must be greater than zero.");
		}

		if (input.EyepieceApparentFieldDeg <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.EyepieceApparentFieldDeg), "Eyepiece apparent field must be greater than zero.");
		}

		if (input.MultiplierFactor <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.MultiplierFactor), "Multiplier factor must be greater than zero.");
		}

		if (input.EyepieceFieldStopMm is <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.EyepieceFieldStopMm), "Field stop must be greater than zero when provided.");
		}
	}
}
