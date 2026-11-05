namespace ObservApp.Shared.Services;

public enum MoonPhaseKind
{
	New,
	WaxingCrescent,
	FirstQuarter,
	WaxingGibbous,
	Full,
	WaningGibbous,
	LastQuarter,
	WaningCrescent
}

public enum SolLunaEclipseBody { Solar, Lunar }

public enum SolLunaEclipseKind { Total, Annular, Partial, Penumbral }

public sealed record SolLunaPathPoint(int Hour, double AltitudeDeg, double AzimuthDeg);

public sealed class SolLunaSunData
{
	public DateTime? Sunrise { get; init; }
	public DateTime? Transit { get; init; }
	public DateTime? Sunset { get; init; }
	public DateTime? CivilDawn { get; init; }
	public DateTime? NauticalDawn { get; init; }
	public DateTime? AstronomicalDawn { get; init; }
	public DateTime? CivilDusk { get; init; }
	public DateTime? NauticalDusk { get; init; }
	public DateTime? AstronomicalDusk { get; init; }
	public TimeSpan? DayLength { get; init; }
	/// <summary>Azimut en grados; <see cref="double.NaN"/> si no hay salida/puesta.</summary>
	public double RiseAzimuthDeg { get; init; } = double.NaN;
	public double SetAzimuthDeg { get; init; } = double.NaN;
	public double TransitAzimuthDeg { get; init; } = double.NaN;
	public double MaxAltitudeDeg { get; init; } = double.NaN;
	/// <summary>Declinación a las 12:00 UTC (grados).</summary>
	public double DeclinationDeg { get; init; }
	/// <summary>Ascensión recta a las 12:00 UTC (horas).</summary>
	public double RightAscensionHours { get; init; }
	/// <summary>Ecuación del tiempo (tiempo solar aparente − tiempo solar medio) en minutos.</summary>
	public double EquationOfTimeMinutes { get; init; }
	public double DistanceAu { get; init; }
	public IReadOnlyList<SolLunaPathPoint> Path { get; init; } = [];
}

public sealed class SolLunaMoonData
{
	public DateTime? Moonrise { get; init; }
	public DateTime? Transit { get; init; }
	public DateTime? Moonset { get; init; }
	public double RiseAzimuthDeg { get; init; } = double.NaN;
	public double SetAzimuthDeg { get; init; } = double.NaN;
	public double MaxAltitudeDeg { get; init; } = double.NaN;
	public double DeclinationDeg { get; init; }
	public double DistanceKm { get; init; }
	public double AngularDiameterDeg { get; init; }
	/// <summary>Porcentaje iluminado (0–100).</summary>
	public double IlluminationPercent { get; init; }
	/// <summary>Longitud eclíptica Luna − Sol (0 = nueva, 180 = llena).</summary>
	public double PhaseAngleDeg { get; init; }
	public double AgeDays { get; init; }
	public MoonPhaseKind Phase { get; init; }
	public DateTime NextNewMoonUtc { get; init; }
	public DateTime NextFullMoonUtc { get; init; }
	public IReadOnlyList<SolLunaPathPoint> Path { get; init; } = [];
}

public sealed record SolLunaMoonPhaseEvent(MoonPhaseKind Phase, DateTime UtcTime);

public sealed record SolLunaEclipseEvent(
	SolLunaEclipseBody Body,
	SolLunaEclipseKind Kind,
	DateTime PeakUtc,
	double Obscuration,     // fracción del disco cubierta en el máximo (Sol: local; Luna: por la umbra)
	DateTime? C1Utc,
	DateTime? C2Utc,
	DateTime? C3Utc,
	DateTime? C4Utc);

public sealed class SolLunaYearEvents
{
	public DateTime MarchEquinoxUtc { get; init; }
	public DateTime JuneSolsticeUtc { get; init; }
	public DateTime SeptemberEquinoxUtc { get; init; }
	public DateTime DecemberSolsticeUtc { get; init; }
	public IReadOnlyList<SolLunaMoonPhaseEvent> MoonPhases { get; init; } = [];
	public IReadOnlyList<SolLunaEclipseEvent> Eclipses { get; init; } = [];
}

public sealed record SolLunaResult(SolLunaSunData Sun, SolLunaMoonData Moon, SolLunaYearEvents Events);

public interface ISolLunaCalculatorService
{
	/// <summary>
	/// Calcula los datos del Sol y la Luna para el día (UTC) indicado y los eventos de su año.
	/// </summary>
	SolLunaResult Calculate(DateTime date, double latitude, double longitude, double altitudeMeters);

	/// <summary>Clasifica la fase lunar a partir del ángulo de fase (0–360°).</summary>
	MoonPhaseKind ClassifyPhase(double phaseAngleDeg);
}
