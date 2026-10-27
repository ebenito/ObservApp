namespace ObservApp.Shared.Services;

public enum TrueFieldMethod
{
	AfovApproximation,
	FieldStop
}

public enum ExitPupilBand
{
	AboveEyeLimit,
	WideField,
	GeneralPurpose,
	Planetary,
	HighPower,
	Overmagnified
}

public sealed record AumentosCalculationInput(
	double ApertureMm,
	double TelescopeFocalLengthMm,
	double EyepieceFocalLengthMm,
	double EyepieceApparentFieldDeg,
	double MultiplierFactor = 1d,
	double? EyepieceFieldStopMm = null);

public sealed record AumentosCalculationResult(
	double EffectiveFocalLengthMm,
	double Magnification,
	double EffectiveFocalRatio,
	double ExitPupilMm,
	double TrueFieldDeg,
	TrueFieldMethod TrueFieldMethod,
	double DawesLimitArcsec,
	double RayleighLimitArcsec,
	double MinUsefulMagnification,
	double MaxUsefulMagnification,
	double RecommendedMagnificationMin,
	double RecommendedMagnificationMax,
	double MagnificationAt5MmExitPupil,
	double MagnificationAt2MmExitPupil,
	double MagnificationAt1MmExitPupil,
	double MagnificationAt05MmExitPupil,
	double LightGainVsNakedEye,
	double LimitingMagnitude,
	ExitPupilBand ExitPupilBand,
	bool IsBelowMinimumUseful,
	bool IsAboveMaximumUseful,
	bool IsFieldStopOutOfTypicalRange);

public interface IAumentosCalculatorService
{
	AumentosCalculationResult Calculate(AumentosCalculationInput input);
}
