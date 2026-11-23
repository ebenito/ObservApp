using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.UbicacionesRegistradas;

public sealed class CoordinateParserTests
{
	private const double Tolerance = 1e-6;

	[Theory]
	[InlineData("40.859167", 40.859167)]
	[InlineData("-2.196667", -2.196667)]
	[InlineData("-2,196667", -2.196667)]
	[InlineData("40°51'33\" N", 40.859167)]          // formato que genera TiempoUbicacionApp
	[InlineData("2°11'48\" O", -2.196667)]           // Oeste con "O"
	[InlineData("2°11'48\" W", -2.196667)]
	[InlineData("41º 03' 34'' N", 41.059444)]
	[InlineData("41°03'34\"N", 41.059444)]           // letra pegada al número
	[InlineData("40.51.33 N", 40.859167)]            // DMS con puntos (valor por defecto de la app)
	[InlineData("2.12.32 O", -2.208889)]
	[InlineData("40:51:33 N", 40.859167)]
	[InlineData("33°27'00\" Sur", -33.45)]
	[InlineData("70°40'00\" Oeste", -70.666667)]
	[InlineData("3°42'13\" Este", 3.703611)]
	[InlineData("-0 30 0", -0.5)]
	public void TryParse_KnownFormats_ReturnsDecimalDegrees(string input, double expected)
	{
		Assert.True(CoordinateParser.TryParse(input, out var value));
		Assert.Equal(expected, value, Tolerance);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("sin coordenadas")]
	[InlineData("40°75'00\" N")]   // minutos fuera de rango
	[InlineData("1 2 3 4")]
	public void TryParse_InvalidInput_ReturnsFalse(string? input)
	{
		Assert.False(CoordinateParser.TryParse(input, out _));
	}

	[Fact]
	public void TryParseLatitude_OutOfRange_ReturnsFalse()
	{
		Assert.False(CoordinateParser.TryParseLatitude("95.5", out _));
		Assert.True(CoordinateParser.TryParseLongitude("95.5", out _));
	}

	[Fact]
	public void TryParseLongitude_OutOfRange_ReturnsFalse()
	{
		Assert.False(CoordinateParser.TryParseLongitude("-181", out _));
	}
}
