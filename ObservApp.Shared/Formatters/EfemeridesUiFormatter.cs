namespace ObservApp.Shared.Formatters;

using System.Globalization;
using Microsoft.Extensions.Localization;
using ObservApp.Shared.Models.PageModels;
using ObservApp.Shared.Services;

public static class EfemeridesUiFormatter
{
	public static string FormatTimeOpt(DateTime? dt)
		=> dt.HasValue ? dt.Value.ToLocalTime().ToString("HH:mm") : "—";

	public static string QualityClass(NightQuality quality) => quality switch
	{
		NightQuality.Good => "eph-quality--good",
		NightQuality.Fair => "eph-quality--fair",
		_ => "eph-quality--poor"
	};

	public static string QualityLabel(IStringLocalizer<ObservApp.Resources.Strings.App> l, NightQuality quality) => quality switch
	{
		NightQuality.Good => l["Eph_Tonight_Quality_Good"],
		NightQuality.Fair => l["Eph_Tonight_Quality_Fair"],
		_ => l["Eph_Tonight_Quality_Poor"]
	};

	public static string EclipseBannerClass(LocalEclipseType type) => type switch
	{
		LocalEclipseType.Total => "eph-eclipse-banner--total",
		LocalEclipseType.Annular => "eph-eclipse-banner--annular",
		_ => "eph-eclipse-banner--partial"
	};

	public static string EclipseEmoji(LocalEclipseType type) => type switch
	{
		LocalEclipseType.Total => "🌑",
		LocalEclipseType.Annular => "💍",
		_ => "🌒"
	};

	public static string EclipseTypeLabel(IStringLocalizer<ObservApp.Resources.Strings.App> l, LocalEclipseType type) => type switch
	{
		LocalEclipseType.Total => l["Eph_Eclipse_Type_Total"],
		LocalEclipseType.Annular => l["Eph_Eclipse_Type_Annular"],
		_ => l["Eph_Eclipse_Type_Partial"]
	};

	public static int DaysUntilEclipse(DateTime eclipseDate)
		=> Math.Max(0, (int)Math.Ceiling((eclipseDate - DateTime.UtcNow).TotalDays));

	public static string DsoTypeLabel(IStringLocalizer<ObservApp.Resources.Strings.App> l, string type) => type switch
	{
		"Nebula" => l["Eph_DSO_Type_Nebula"],
		"OpenCluster" => l["Eph_DSO_Type_OpenCluster"],
		"GlobularCluster" => l["Eph_DSO_Type_GlobularCluster"],
		_ => l["Eph_DSO_Type_Galaxy"]
	};

	public static string DsoAccentClass(string type) => type switch
	{
		"Nebula" => "eph-dso-card--nebula",
		"OpenCluster" => "eph-dso-card--open",
		"GlobularCluster" => "eph-dso-card--globular",
		_ => "eph-dso-card--galaxy"
	};

	public static string BuildDsoVisibilityStyle(DsoVisibility item)
		=> $"left:{item.VisibleStartPercent.ToString("F2", CultureInfo.InvariantCulture)}%;width:{item.VisibleWidthPercent.ToString("F2", CultureInfo.InvariantCulture)}%;";

	public static string BuildDsoThumbnailUrl(DsoEntry dso)
	{
		var fov = DsoFieldOfView(dso).ToString("F3", CultureInfo.InvariantCulture);
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);

		return $"https://alasky.cds.unistra.fr/hips-image-services/hips2fits?hips=CDS%2FP%2FDSS2%2Fcolor&width=320&height=220&projection=TAN&fov={fov}&ra={ra}&dec={dec}&format=jpg";
	}

	public static string BuildDsoLargeImageUrl(DsoEntry dso)
	{
		var fov = DsoFieldOfView(dso).ToString("F3", CultureInfo.InvariantCulture);
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);

		// Imagen de mayor resolución para el lightbox
		return $"https://alasky.cds.unistra.fr/hips-image-services/hips2fits?hips=CDS%2FP%2FDSS2%2Fcolor&width=1200&height=900&projection=TAN&fov={fov}&ra={ra}&dec={dec}&format=jpg";
	}

	public static string DsoThumbnailAltText(DsoEntry dso)
		=> $"{dso.Id} {dso.Name}";

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

	private static double DsoFieldOfView(DsoEntry dso) => dso.Type switch
	{
		"Nebula" => 1.2,
		"OpenCluster" => 1.5,
		"GlobularCluster" => 0.6,
		_ => 0.8
	};
}
