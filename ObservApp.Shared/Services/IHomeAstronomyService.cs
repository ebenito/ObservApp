namespace ObservApp.Shared.Services;

public sealed class HomeAstronomySnapshot
{
	public DateTime? Sunrise { get; init; }
	public DateTime? Sunset { get; init; }
	public TimeSpan? DayLength { get; init; }
	public DateTime? AstroTwilightEnd { get; init; }
	public bool IsAstroNight { get; init; }
	public bool IsDaytime { get; init; }
	public DateTime? Moonrise { get; init; }
	public DateTime? Moonset { get; init; }
	public double MoonPhaseAngle { get; init; }
	public double MoonIllumination { get; init; }
	public double MoonAge { get; init; }
	public DateTime? NextFullMoonUtc { get; init; }
}

public interface IHomeAstronomyService
{
	HomeAstronomySnapshot? CalculateSnapshot(double latitude, double longitude, double altitudeMeters, DateTime nowUtc);
}
