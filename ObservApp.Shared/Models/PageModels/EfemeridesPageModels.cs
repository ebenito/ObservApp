namespace ObservApp.Shared.Models.PageModels;

using CosineKitty;

public sealed class PlanetData
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

public sealed class EphEvent
{
	public string Emoji { get; init; } = string.Empty;
	public DateTime Date { get; init; }
	public string Description { get; init; } = string.Empty;
}

public enum NightQuality
{
	Good,
	Fair,
	Poor
}

public sealed class TonightData
{
	public double MoonIlluminationPct { get; init; }
	public string MoonPhaseName { get; init; } = string.Empty;
	public string MoonPhaseEmoji { get; init; } = string.Empty;
	public DateTime? AstroNightStart { get; init; }
	public DateTime? MoonRise { get; init; }
	public DateTime? MoonSet { get; init; }
	public NightQuality Quality { get; init; }
}

public sealed record DsoEntry(
	string Id,
	string Name,
	string Constellation,
	string Type,
	string Emoji,
	double RaDeg,
	double DecDeg,
	double Magnitude,
	string Description);

public sealed class DsoVisibility
{
	public DsoEntry Dso { get; init; } = null!;
	public bool IsVisibleTonight { get; init; }
	public double MaxAltitude { get; init; }
	public double AzimuthAtMax { get; init; }
	public DateTime? VisibleFrom { get; init; }
	public DateTime? VisibleUntil { get; init; }
	public DateTime? BestViewingTime { get; init; }
	public double VisibleStartPercent { get; init; }
	public double VisibleWidthPercent { get; init; }
	public bool ThumbnailFailed { get; set; }
}
