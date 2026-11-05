namespace ObservApp.Shared.Services;

public sealed class EclipseExposureCalculatorService : IEclipseExposureCalculatorService
{
	// El Sol subtiende ~0,53°: su imagen en el plano focal mide F / 109
	private const double FocalLengthPerSolarDiameter = 109d;

	private sealed record PhaseDefinition(EclipsePhotoPhase Phase, int Q, bool IsTotality, bool IsPartial);

	private static readonly PhaseDefinition[] Phases =
	{
		new(EclipsePhotoPhase.PartialOd50,  -6, false, true),
		new(EclipsePhotoPhase.PartialOd40,  -4, false, true),
		new(EclipsePhotoPhase.BailysBeads,   2, false, false),
		new(EclipsePhotoPhase.Chromosphere,  3, true,  false),
		new(EclipsePhotoPhase.Prominences,   4, true,  false),
		new(EclipsePhotoPhase.Corona01,      5, true,  false),
		new(EclipsePhotoPhase.Corona05,      6, true,  false),
		new(EclipsePhotoPhase.Corona10,      7, true,  false),
		new(EclipsePhotoPhase.Corona20,      8, true,  false),
		new(EclipsePhotoPhase.Corona40,      9, true,  false),
		new(EclipsePhotoPhase.Earthshine,   10, true,  false),
	};

	// Velocidades de obturación estándar (segundos)
	private static readonly double[] StandardSpeeds =
	{
		1.0 / 8000, 1.0 / 4000, 1.0 / 2000, 1.0 / 1000, 1.0 / 500,
		1.0 / 250, 1.0 / 125, 1.0 / 60, 1.0 / 30, 1.0 / 15,
		1.0 / 8, 1.0 / 4, 1.0 / 2, 1.0, 2.0
	};

	public EclipseExposureResult Calculate(EclipseExposureInput input)
	{
		ValidateInput(input);

		// Diámetro del Sol en el plano focal
		var solarDiameter = input.FocalLengthMm / FocalLengthPerSolarDiameter;
		var coverage = solarDiameter / input.SensorWidthMm * 100d;

		// FOV diagonal (grados)
		var diagonalMm = Math.Sqrt(input.SensorWidthMm * input.SensorWidthMm + input.SensorHeightMm * input.SensorHeightMm);
		var diagonalFov = 2d * Math.Atan(diagonalMm / (2d * input.FocalLengthMm)) * (180d / Math.PI);

		var rows = Phases
			.Select(p =>
			{
				var exact = ExposureSeconds(input.FNumber, input.Iso, p.Q);
				return new EclipseExposureRow(p.Phase, p.Q, p.IsTotality, p.IsPartial, exact, NearestStandardSpeed(exact));
			})
			.ToList();

		return new EclipseExposureResult(solarDiameter, coverage, diagonalFov, rows);
	}

	/// <summary>t = f² / (ISO · 2^Q)</summary>
	private static double ExposureSeconds(double fNumber, int iso, int q)
		=> fNumber * fNumber / (iso * Math.Pow(2d, q));

	/// <summary>Velocidad estándar más cercana en escala logarítmica (pasos de diafragma).</summary>
	private static double NearestStandardSpeed(double seconds)
		=> StandardSpeeds.MinBy(s => Math.Abs(Math.Log(s) - Math.Log(seconds)));

	private static void ValidateInput(EclipseExposureInput input)
	{
		if (input.FocalLengthMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.FocalLengthMm), "Focal length must be greater than zero.");
		}

		if (input.FNumber <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.FNumber), "F-number must be greater than zero.");
		}

		if (input.Iso <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.Iso), "ISO must be greater than zero.");
		}

		if (input.SensorWidthMm <= 0 || input.SensorHeightMm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(input.SensorWidthMm), "Sensor dimensions must be greater than zero.");
		}
	}
}
