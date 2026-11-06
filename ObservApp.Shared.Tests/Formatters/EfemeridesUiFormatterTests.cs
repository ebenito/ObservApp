using System.Globalization;
using ObservApp.Shared.Formatters;
using ObservApp.Shared.Models.PageModels;
using Xunit;

namespace ObservApp.Shared.Tests.Formatters;

public sealed class EfemeridesUiFormatterTests
{
	[Fact]
	public void BuildDsoThumbnailUrl_GalaxyNorth_UsesLegacySurveys()
	{
		var dso = CreateDso(type: "Galaxy", decDeg: 41.1);

		var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

		Assert.StartsWith("https://www.legacysurvey.org/viewer/jpeg-cutout", url, StringComparison.Ordinal);
	}

	[Fact]
	public void BuildDsoThumbnailUrl_GalaxySouth_UsesStsciDss()
	{
		var dso = CreateDso(type: "Galaxy", decDeg: -70);

		var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

		Assert.StartsWith("https://archive.stsci.edu/cgi-bin/dss_search", url, StringComparison.Ordinal);
	}

	[Fact]
	public void BuildDsoThumbnailUrl_Nebula_UsesStsciDss()
	{
		var dso = CreateDso(type: "Nebula", decDeg: 20);

		var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

		Assert.StartsWith("https://archive.stsci.edu/cgi-bin/dss_search", url, StringComparison.Ordinal);
	}

	[Fact]
	public void BuildDsoThumbnailUrl_OpenCluster_UsesStsciDss()
	{
		var dso = CreateDso(type: "OpenCluster", decDeg: 20);

		var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

		Assert.StartsWith("https://archive.stsci.edu/cgi-bin/dss_search", url, StringComparison.Ordinal);
	}

	[Fact]
	public void BuildDsoThumbnailUrl_UsesInvariantCultureForNumbers()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		var previousUiCulture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = new CultureInfo("es-ES");
		CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

		try
		{
			var dso = CreateDso(type: "Galaxy", raDeg: 192.7, decDeg: 41.1);

			var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

			Assert.Contains("ra=192.7000", url, StringComparison.Ordinal);
			Assert.Contains("dec=41.1000", url, StringComparison.Ordinal);
			Assert.DoesNotContain("192,7000", url, StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
			CultureInfo.CurrentUICulture = previousUiCulture;
		}
	}

	[Fact]
	public void BuildDsoThumbnailUrl_Stsci_ClampsWidthArcminTo60()
	{
		var dso = CreateDso(type: "Nebula", decDeg: 10);

		var url = EfemeridesUiFormatter.BuildDsoThumbnailUrl(dso);

		Assert.Contains("&w=60.000&", url, StringComparison.Ordinal);
		Assert.Contains("&h=41.250&", url, StringComparison.Ordinal);
	}

	[Fact]
	public void BuildDsoLargeImageUrl_Legacy_ClampsSizeTo512()
	{
		var dso = CreateDso(type: "Galaxy", decDeg: 41.1);

		var url = EfemeridesUiFormatter.BuildDsoLargeImageUrl(dso);

		Assert.Contains("size=512", url, StringComparison.Ordinal);
		Assert.Contains("pixscale=5.625", url, StringComparison.Ordinal);
	}

	private static DsoEntry CreateDso(string type, double decDeg, double raDeg = 192.7)
		=> new(
			Id: "M94",
			Name: "Test",
			Constellation: "CVn",
			Type: type,
			Emoji: "✨",
			RaDeg: raDeg,
			DecDeg: decDeg,
			Magnitude: 8.0,
			Description: "Test");
}
