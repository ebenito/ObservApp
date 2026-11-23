namespace ObservApp.Shared.Services;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Interpreta coordenadas en texto con los formatos que guarda TiempoUbicacionApp.
/// Acepta decimal (<c>40.859167</c>, <c>-2,196667</c>), DMS con símbolos
/// (<c>41°03'34" N</c>, <c>41º 03' 34'' N</c>), DMS con puntos (<c>40.51.33 N</c>) o dos puntos
/// (<c>40:51:33 N</c>) y las palabras Norte, Sur, Este y Oeste. Sur y Oeste (<c>S</c>, <c>W</c>, <c>O</c>)
/// dan valores negativos.
/// </summary>
public static partial class CoordinateParser
{
	/// <summary>Convierte una latitud en texto a grados decimales en [-90, 90].</summary>
	public static bool TryParseLatitude(string? input, out double value) =>
		TryParse(input, out value) && value is >= -90 and <= 90;

	/// <summary>Convierte una longitud en texto a grados decimales en [-180, 180].</summary>
	public static bool TryParseLongitude(string? input, out double value) =>
		TryParse(input, out value) && value is >= -180 and <= 180;

	/// <summary>Convierte una coordenada en texto a grados decimales sin comprobar el rango.</summary>
	public static bool TryParse(string? input, out double value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(input))
		{
			return false;
		}

		var s = input.Trim();

		// Decimal con coma ("-2,196667"): una sola coma y ningún otro separador.
		if (DecimalCommaRegex().IsMatch(s))
		{
			s = s.Replace(',', '.');
		}

		// Separar la letra de dirección pegada a los números ("33N", "N40") para que \b la encuentre.
		s = DigitLetterRegex().Replace(s, "$1 $2");
		s = LetterDigitRegex().Replace(s, "$1 $2");

		s = NorthRegex().Replace(s, "N");
		s = SouthRegex().Replace(s, "S");
		s = EastRegex().Replace(s, "E");
		s = WestRegex().Replace(s, "W");
		s = WestLetterORegex().Replace(s, "W");

		// Varios puntos ("40.51.33") son separadores DMS, no decimales.
		if (s.Count(c => c == '.') >= 2)
		{
			s = s.Replace('.', ' ');
		}

		s = s.Replace('°', ' ')
			.Replace('º', ' ')
			.Replace(':', ' ')
			.Replace(',', ' ')
			.Replace("''", " ")
			.Replace('″', ' ')
			.Replace('′', ' ')
			.Replace('“', ' ')
			.Replace('”', ' ')
			.Replace('"', ' ')
			.Replace('\'', ' ');

		var dirMatch = DirectionRegex().Match(s);
		var dir = dirMatch.Success ? char.ToUpperInvariant(dirMatch.Value[0]) : '\0';

		var numbersOnly = LettersRegex().Replace(s, " ");
		var nums = NumberRegex().Matches(numbersOnly)
			.Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))
			.ToArray();

		if (nums.Length is 0 or > 3)
		{
			return false;
		}

		var negative = nums[0] < 0 || (nums[0] == 0 && numbersOnly.TrimStart().StartsWith('-'));
		var degrees = Math.Abs(nums[0]);
		var minutes = nums.Length > 1 ? nums[1] : 0;
		var seconds = nums.Length > 2 ? nums[2] : 0;

		if (minutes < 0 || minutes >= 60 || seconds < 0 || seconds >= 60)
		{
			return false;
		}

		var result = degrees + minutes / 60.0 + seconds / 3600.0;

		if (negative || dir is 'S' or 'W')
		{
			result = -result;
		}

		value = result;
		return true;
	}

	[GeneratedRegex(@"^\s*-?\d+,\d+\s*$")]
	private static partial Regex DecimalCommaRegex();

	[GeneratedRegex(@"(\d)([A-Za-z])")]
	private static partial Regex DigitLetterRegex();

	[GeneratedRegex(@"([A-Za-z])(\d)")]
	private static partial Regex LetterDigitRegex();

	[GeneratedRegex(@"\bnorte\b", RegexOptions.IgnoreCase)]
	private static partial Regex NorthRegex();

	[GeneratedRegex(@"\bsur\b", RegexOptions.IgnoreCase)]
	private static partial Regex SouthRegex();

	[GeneratedRegex(@"\beste\b", RegexOptions.IgnoreCase)]
	private static partial Regex EastRegex();

	[GeneratedRegex(@"\boeste\b", RegexOptions.IgnoreCase)]
	private static partial Regex WestRegex();

	[GeneratedRegex(@"\bO\b", RegexOptions.IgnoreCase)]
	private static partial Regex WestLetterORegex();

	[GeneratedRegex(@"\b([NSEW])\b", RegexOptions.IgnoreCase)]
	private static partial Regex DirectionRegex();

	[GeneratedRegex(@"[A-Za-z]")]
	private static partial Regex LettersRegex();

	[GeneratedRegex(@"-?\d+(\.\d+)?")]
	private static partial Regex NumberRegex();
}
