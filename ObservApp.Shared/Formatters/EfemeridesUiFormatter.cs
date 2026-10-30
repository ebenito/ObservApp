namespace ObservApp.Shared.Formatters;

using System.Globalization;
using Microsoft.Extensions.Localization;
using ObservApp.Shared.Models.PageModels;
using ObservApp.Shared.Services;

public enum DsoImageProvider
{
	StsciDss,
	SkyView,
	CdsHips2Fits,
	LegacySurveys
}

public static class EfemeridesUiFormatter
{
	/// <summary>
	/// Si no es null, fuerza el uso de este proveedor para todos los objetos
	/// (útil para reactivar CDS cuando vuelva el servicio o para probar Legacy Surveys).
	/// Con null se aplican las reglas de <see cref="SelectProvider"/>.
	/// </summary>
	public static DsoImageProvider? ForcedProvider { get; set; } = null;

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
		=> BuildImageUrl(dso, width: 320, height: 220);

	public static string BuildDsoLargeImageUrl(DsoEntry dso)
		=> BuildImageUrl(dso, width: 900, height: 675);

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

	private static string BuildImageUrl(DsoEntry dso, int width, int height)
	{
		var provider = ForcedProvider ?? SelectProvider(dso);
		return provider switch
		{
			DsoImageProvider.StsciDss => BuildStsciDssUrl(dso, width, height),
			DsoImageProvider.LegacySurveys => BuildLegacySurveysUrl(dso, width, height),
			DsoImageProvider.SkyView => BuildSkyViewUrl(dso, width, height),
			_ => BuildCdsHips2FitsUrl(dso, width, height)
		};
	}

	private static DsoImageProvider SelectProvider(DsoEntry dso)
	{
		// Reglas editables de selección de proveedor (ambos cubren todo el cielo):
		// - Galaxias => NASA SkyView (DSS2 Red).
		// - Resto de tipos => STScI DSS.
		// Respaldos disponibles con ForcedProvider: CdsHips2Fits (cuando vuelva el servicio)
		// y LegacySurveys (color, pero solo con cobertura parcial: ver nota en BuildLegacySurveysUrl).
		if (IsGalaxy(dso))
		{
			return DsoImageProvider.SkyView;
		}

		return DsoImageProvider.StsciDss;
	}

	private static bool IsGalaxy(DsoEntry dso)
		=> string.Equals(dso.Type, "Galaxy", StringComparison.OrdinalIgnoreCase);

	private static string BuildStsciDssUrl(DsoEntry dso, int width, int height)
	{
		var fovDeg = DsoFieldOfView(dso);
		// STScI DSS limita el campo a 60' por lado.
		var wArcmin = Math.Min(fovDeg * 60d, 60d);
		var hArcmin = wArcmin * height / width;
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);
		var w = wArcmin.ToString("F3", CultureInfo.InvariantCulture);
		var h = hArcmin.ToString("F3", CultureInfo.InvariantCulture);

		return $"https://archive.stsci.edu/cgi-bin/dss_search?v=poss2ukstu_red&r={ra}&d={dec}&e=J2000&h={h}&w={w}&f=gif&c=none&fov=NONE";
	}

	// Respaldo (solo con ForcedProvider). Cobertura parcial: DR9 no cubre el plano galáctico (|b| < ~20°)
	// ni el sur lejano (Dec < ~-20°); fuera de la huella devuelve imágenes negras. DR10 solo llega hasta Dec ~ +30°.
	private static string BuildLegacySurveysUrl(DsoEntry dso, int width, int height)
	{
		var fovDeg = DsoFieldOfView(dso);
		// Recorte cuadrado, máximo 512 px.
		var size = Math.Min(Math.Max(width, height), 512);
		var pixscale = fovDeg * 3600d / size;
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);
		var pix = pixscale.ToString("F3", CultureInfo.InvariantCulture);

		return $"https://www.legacysurvey.org/viewer/jpeg-cutout?ra={ra}&dec={dec}&layer=ls-dr9&pixscale={pix}&size={size}";
	}

	// NASA SkyView (DSS2 Red, monocromo). Verificado: devuelve la imagen directamente. Puede ser lento.
	private static string BuildSkyViewUrl(DsoEntry dso, int width, int height)
	{
		var fovDeg = DsoFieldOfView(dso);
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);
		var sizeW = fovDeg.ToString("F3", CultureInfo.InvariantCulture);
		var sizeH = (fovDeg * height / width).ToString("F3", CultureInfo.InvariantCulture);

		return $"https://skyview.gsfc.nasa.gov/cgi-bin/images?Survey=DSS2+Red&position={ra},{dec}&Size={sizeW},{sizeH}&Pixels={width},{height}&Return=JPEG";
	}

	private static string BuildCdsHips2FitsUrl(DsoEntry dso, int width, int height)
	{
		var fov = DsoFieldOfView(dso).ToString("F3", CultureInfo.InvariantCulture);
		var ra = dso.RaDeg.ToString("F4", CultureInfo.InvariantCulture);
		var dec = dso.DecDeg.ToString("F4", CultureInfo.InvariantCulture);

		return $"https://alasky.cds.unistra.fr/hips-image-services/hips2fits?hips=CDS%2FP%2FDSS2%2Fcolor&width={width}&height={height}&projection=TAN&fov={fov}&ra={ra}&dec={dec}&format=jpg";
	}

	private static double DsoFieldOfView(DsoEntry dso) => dso.Type switch
	{
		"Nebula" => 1.2,
		"OpenCluster" => 1.5,
		"GlobularCluster" => 0.6,
		_ => 0.8
	};
}