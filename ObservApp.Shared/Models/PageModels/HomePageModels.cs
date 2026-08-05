namespace ObservApp.Shared.Models.PageModels;

public sealed class AstroSnapshot
{
	public DateTime? Sunrise { get; init; }
	public DateTime? Sunset { get; init; }
	public string DayLength { get; init; } = "—";
	public DateTime? AstroTwilightEnd { get; init; }
	public bool IsAstroNight { get; init; }
	public bool IsDaytime { get; init; }
	public DateTime? Moonrise { get; init; }
	public DateTime? Moonset { get; init; }
	public string MoonPhaseName { get; init; } = string.Empty;
	public string MoonEmoji { get; init; } = "🌑";
	public double MoonIllumination { get; init; }
	public double MoonAge { get; init; }
	public string NextFullMoon { get; init; } = string.Empty;
}
