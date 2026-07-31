namespace ObservApp.Shared.Models;

public class ObservationTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TargetType Type { get; set; } = TargetType.Other;
    public string Constellation { get; set; } = string.Empty;
    public string Eyepiece { get; set; } = string.Empty;
    public int? Magnification { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int Rating { get; set; }
}

public enum TargetType
{
    Planet,
    Moon,
    DeepSkyObject,
    DoubleStar,
    Asteroid,
    Comet,
    Other
}
