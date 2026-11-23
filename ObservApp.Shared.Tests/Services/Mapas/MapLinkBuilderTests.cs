using System.Globalization;
using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Mapas;

public sealed class MapLinkBuilderTests
{
	[Fact]
	public void GoogleMaps_UsesDotDecimalSeparatorWhateverTheCulture()
	{
		var previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("es-ES");
		try
		{
			Assert.Equal(
				"https://www.google.com/maps?q=40.859167,-2.196667&z=15",
				MapLinkBuilder.GoogleMaps(40.8591666, -2.1966666));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void OpenStreetMap_PutsMarkerAndViewport()
	{
		Assert.Equal(
			"https://www.openstreetmap.org/?mlat=28.2724&mlon=-16.6425#map=12/28.2724/-16.6425",
			MapLinkBuilder.OpenStreetMap(28.2724, -16.6425, 12));
	}
}
