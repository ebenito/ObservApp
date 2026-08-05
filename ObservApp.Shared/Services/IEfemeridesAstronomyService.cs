namespace ObservApp.Shared.Services;

using CosineKitty;
using ObservApp.Shared.Models;

public enum EphNightQuality
{
	Good,
	Fair,
	Poor
}

public sealed class EphPlanetData
{
	public string Name { get; init; } = string.Empty;
	public string Emoji { get; init; } = string.Empty;
	public Body Body { get; init; }
	public bool IsVisible { get; init; }
	public double MaxAltitude { get; init; }
	public double AzimuthAtMax { get; init; }
	public DateTime? RiseTime { get; init; }
	public DateTime? SetTime { get; init; }
	public double DistanceAU { get; init; }
	public double IlluminationPct { get; init; }
}

public sealed class EphEventData
{
	public string Emoji { get; init; } = string.Empty;
	public DateTime Date { get; init; }
	public string Description { get; init; } = string.Empty;
}

public sealed class EphTonightData
{
	public double MoonIlluminationPct { get; init; }
	public double MoonPhaseAngle { get; init; }
	public DateTime? AstroNightStart { get; init; }
	public DateTime? MoonRise { get; init; }
	public DateTime? MoonSet { get; init; }
	public EphNightQuality Quality { get; init; }
}

public sealed class EphNightWindow
{
	public DateTime StartUtc { get; init; }
	public DateTime EndUtc { get; init; }
}

public sealed class EphDsoCoordinate
{
	public string Id { get; init; } = string.Empty;
	public double RaDeg { get; init; }
	public double DecDeg { get; init; }
	public double Magnitude { get; init; }
}

public sealed class EphDsoVisibility
{
	public string Id { get; init; } = string.Empty;
	public bool IsVisibleTonight { get; init; }
	public double MaxAltitude { get; init; }
	public double AzimuthAtMax { get; init; }
	public DateTime? VisibleFrom { get; init; }
	public DateTime? VisibleUntil { get; init; }
	public DateTime? BestViewingTime { get; init; }
	public double VisibleStartPercent { get; init; }
	public double VisibleWidthPercent { get; init; }
}

public sealed class EphComputationResult
{
	public List<EphPlanetData> Planets { get; init; } = new();
	public List<EphEventData> Events { get; init; } = new();
	public EphTonightData? Tonight { get; init; }
}

public interface IEfemeridesAstronomyService
{
	EphComputationResult Compute(DateTime date, double latitude, double longitude, double altitudeMeters);
	(EclipseDefinition? Eclipse, LocalEclipseResult? Result) FindNextVisibleEclipse(
		DateTime nowUtc,
		double latitude,
		double longitude,
		double altitudeMeters,
		int yearsAhead = 2);

	(EphNightWindow? Window, List<EphDsoVisibility> Visible) ComputeDsoVisibility(
		DateTime date,
		double latitude,
		double longitude,
		double altitudeMeters,
		IEnumerable<EphDsoCoordinate> dsos,
		CancellationToken cancellationToken = default);
}
