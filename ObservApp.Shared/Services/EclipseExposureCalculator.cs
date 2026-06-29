using ObservApp.Shared.Models;

namespace ObservApp.Shared.Services;

/// <summary>
/// Calcula la tabla de exposición NASA-Q para fotografía de eclipses y
/// resuelve qué fila de esa tabla corresponde a cada evento calculado por
/// <see cref="IEclipseCalculatorService"/>. Reutiliza la MISMA fórmula y
/// tabla de fases que <c>CalculadoraEclipse.razor</c> (t = f² / (ISO · 2^Q)),
/// de forma que la Calculadora de Exposición y el Timer de Eclipse manejan
/// siempre los mismos números — no hay dos fuentes de verdad para el cálculo
/// fotométrico, solo para la presentación visual de cada página.
///
/// NOTA: <c>CalculadoraEclipse.razor</c> mantiene su propia copia de la
/// tabla de fases para no arriesgar cambiar su comportamiento ya probado.
/// Si en el futuro se ajusta una tabla, hay que ajustar también la otra (o
/// migrar esa página para que consuma este método).
/// </summary>
public static class EclipseExposureCalculator
{
	/// <summary>Una fila de la tabla NASA-Q ya resuelta a un <see cref="ExposureSetting"/> concreto.</summary>
	public sealed record PhaseExposure(
		string PhaseName, int Q, bool IsTotality, bool IsPartial, ExposureSetting Setting);

	private static readonly (string Name, int Q, bool IsTotality, bool IsPartial)[] Phases =
	{
		("OD 5.0 (parcial)",  -6, false, true),
		("OD 4.0 (parcial)",  -4, false, true),
		("Baily's Beads",      2, false, false),
		("Cromosfera",         3, true,  false),
		("Protuberancias",     4, true,  false),
		("Corona (0.1R☉)",    5, true,  false),
		("Corona (0.5R☉)",    6, true,  false),
		("Corona (1.0R☉)",    7, true,  false),
		("Corona (2.0R☉)",    8, true,  false),
		("Corona (4.0R☉)",    9, true,  false),
		("Tierra iluminada", 10, true,  false),
	};

	private static readonly double[] StandardSpeeds =
	{
		1.0/8000, 1.0/4000, 1.0/2000, 1.0/1000, 1.0/500,
		1.0/250, 1.0/125, 1.0/60, 1.0/30, 1.0/15,
		1.0/8, 1.0/4, 1.0/2, 1.0, 2.0
	};

	/// <summary>
	/// Calcula la tabla completa de 11 fases para el equipo indicado.
	/// Devuelve lista vacía si los parámetros no son válidos (apertura/ISO ≤ 0).
	/// </summary>
	public static List<PhaseExposure> BuildTable(double apertureFNumber, int iso)
	{
		if (apertureFNumber <= 0 || iso <= 0) return new();

		var result = new List<PhaseExposure>(Phases.Length);
		foreach (var (name, q, isTotality, isPartial) in Phases)
		{
			var exact = (apertureFNumber * apertureFNumber) / (iso * Math.Pow(2.0, q));
			var nearest = StandardSpeeds.MinBy(s => Math.Abs(Math.Log(s) - Math.Log(exact)));

			var shutter = nearest >= 1.0
				? nearest.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "\""
				: $"1/{(int)Math.Round(1.0 / nearest)}";

			result.Add(new PhaseExposure(name, q, isTotality, isPartial,
				new ExposureSetting(shutter, iso, $"f/{apertureFNumber:0.#}")));
		}
		return result;
	}

	/// <summary>
	/// Las 8 exposiciones de totalidad (Cromosfera → Tierra iluminada),
	/// ordenadas de la más corta a la más larga. Este orden hace que la
	/// primera toma de cada repetición del bracket sea siempre la más corta
	/// — apropiada justo en el instante C2, cuando aún hay brillo residual —
	/// y la última la más larga, apropiada para la corona externa más débil
	/// ya en plena totalidad.
	/// </summary>
	public static List<ExposureSetting> GetTotalityBracket(List<PhaseExposure> table)
		=> table.Where(p => p.IsTotality).OrderBy(p => p.Q).Select(p => p.Setting).ToList();

	/// <summary>
	/// Resuelve qué fila de la tabla corresponde a un evento puntual (no de
	/// totalidad) del eclipse. Devuelve <c>null</c> para los eventos que
	/// deliberadamente NO disparan una foto propia.
	/// </summary>
	public static ExposureSetting? ResolveSingleEventExposure(EclipseEventKind kind, List<PhaseExposure> table)
	{
		// ── Decisión de diseño ───────────────────────────────────────────
		// Maximum y C3 no disparan foto propia:
		//  · Maximum solo sigue usándose para el aviso TTS "Máximo eclipse"
		//    — el bracket lanzado en C2 ya cubre ese instante con su propia
		//    cadencia, relanzarlo aquí solo lo reiniciaría sin necesidad.
		//  · C3 marca el fin de la totalidad, pero se deja un margen de
		//    seguridad de unos segundos para no cortar el bracket en curso
		//    justo en el instante más interesante. Es DiamondRingEnd
		//    (~5 s después) quien efectivamente cancela el bracket al
		//    lanzar su propia foto.
		if (kind is EclipseEventKind.Maximum or EclipseEventKind.C3)
			return null;

		var phaseName = kind switch
		{
			EclipseEventKind.C1 or EclipseEventKind.C4 => "OD 5.0 (parcial)",
			EclipseEventKind.ShadowBands => "OD 4.0 (parcial)",
			EclipseEventKind.BailyBeadsStart
				or EclipseEventKind.BailyBeadsEnd
				or EclipseEventKind.DiamondRingStart
				or EclipseEventKind.DiamondRingEnd => "Baily's Beads",
			_ => null
		};

		return phaseName is null
			? null
			: table.FirstOrDefault(p => p.PhaseName == phaseName)?.Setting;
	}
}