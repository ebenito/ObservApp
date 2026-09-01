namespace ObservApp.Shared.Models.PageModels;

public sealed record DsoFilterOption(
	string Value,
	string Label,
	string Icon,
	string Subtitle,
	string AvatarClass,
	string? Badge = null);
