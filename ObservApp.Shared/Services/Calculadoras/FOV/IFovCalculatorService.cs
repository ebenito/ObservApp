namespace ObservApp.Shared.Services;

public enum FovExitPupilQuality
{
	/// <summary>Pupila de salida &gt; 7 mm: se pierde luz (fuera del rango del ojo adaptado).</summary>
	AboveEyeLimit,
	/// <summary>5–7 mm: campo amplio, cielo profundo.</summary>
	Good,
	/// <summary>2–5 mm: uso general y planetario.</summary>
	Planetary,
	/// <summary>&lt; 2 mm: alto aumento.</summary>
	Narrow
}

public enum FovReferenceSize
{
	SmallerThanFullMoon,
	AboutFullMoon,
	SmallerThanPleiades,
	AboutPleiades,
	LargerThan3Deg
}

public sealed record FovCalculationInput(
	double TelescopeFocalLengthMm,
	double ApertureMm,
	double EyepieceFocalLengthMm,
	double EyepieceApparentFieldDeg);

public sealed record FovCalculationResult(
	double Magnification,
	double TrueFieldDeg,
	double TrueFieldArcmin,
	double ExitPupilMm,
	double FocalRatio,
	bool IsNearMaxMagnification,
	FovExitPupilQuality ExitPupilQuality,
	FovReferenceSize ReferenceSize);

public interface IFovCalculatorService
{
	FovCalculationResult Calculate(FovCalculationInput input);
}
