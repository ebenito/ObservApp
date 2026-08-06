namespace ObservApp.Shared.Formatters;

using Microsoft.Extensions.Localization;
using ObservApp.Shared.Services;

public static class HomeUiFormatter
{
	public static string FormatTime(DateTime? dt)
		=> dt.HasValue ? dt.Value.ToLocalTime().ToString("HH:mm") : "—";

	public static string FormatDuration(TimeSpan? ts)
		=> ts.HasValue ? $"{(int)ts.Value.TotalHours}h {ts.Value.Minutes:D2}m" : "—";

	public static string MoonPhaseName(IStringLocalizer<ObservApp.Resources.Strings.App> l, double angle) => angle switch
	{
		< 22.5 => l["SolLuna_Phase_New"],
		< 67.5 => l["SolLuna_Phase_WaxCrescent"],
		< 112.5 => l["SolLuna_Phase_FirstQuarter"],
		< 157.5 => l["SolLuna_Phase_WaxGibbous"],
		< 202.5 => l["SolLuna_Phase_Full"],
		< 247.5 => l["SolLuna_Phase_WanGibbous"],
		< 292.5 => l["SolLuna_Phase_LastQuarter"],
		< 337.5 => l["SolLuna_Phase_WanCrescent"],
		_ => l["SolLuna_Phase_New"]
	};

	public static string MoonPhaseEmoji(double angle) => angle switch
	{
		< 22.5 => "🌑",
		< 67.5 => "🌒",
		< 112.5 => "🌓",
		< 157.5 => "🌔",
		< 202.5 => "🌕",
		< 247.5 => "🌖",
		< 292.5 => "🌗",
		< 337.5 => "🌘",
		_ => "🌑"
	};

	public static string EclipseTypeName(IStringLocalizer<ObservApp.Resources.Strings.App> l, EclipseType type) => type switch
	{
		EclipseType.Total => l["SolLuna_Eclipse_Total"],
		EclipseType.Annular => l["SolLuna_Eclipse_Annular"],
		EclipseType.Hybrid => l["SolLuna_Eclipse_Annular"],
		_ => l["SolLuna_Eclipse_Partial"]
	};
}
