namespace ObservApp.Shared.Services;

/// <summary>
/// Fases fotografiables de un eclipse solar con su valor Q de brillo (tabla de F. Espenak, NASA).
/// </summary>
public enum EclipsePhotoPhase
{
	PartialOd50,
	PartialOd40,
	BailysBeads,
	Chromosphere,
	Prominences,
	Corona01,
	Corona05,
	Corona10,
	Corona20,
	Corona40,
	Earthshine
}

public sealed record EclipseExposureInput(
	double FocalLengthMm,
	double FNumber,
	int Iso,
	double SensorWidthMm,
	double SensorHeightMm);

public sealed record EclipseExposureRow(
	EclipsePhotoPhase Phase,
	int Q,
	bool IsTotality,
	bool IsPartial,
	double ExactSeconds,
	double SuggestedSeconds);

public sealed record EclipseExposureResult(
	double SolarImageDiameterMm,
	double SensorWidthCoveragePercent,
	double DiagonalFieldDeg,
	IReadOnlyList<EclipseExposureRow> Rows);

public interface IEclipseExposureCalculatorService
{
	EclipseExposureResult Calculate(EclipseExposureInput input);
}
