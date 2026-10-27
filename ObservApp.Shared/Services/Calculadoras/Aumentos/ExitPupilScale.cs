namespace ObservApp.Shared.Services;

public static class ExitPupilScale
{
	public const double MinPupilMm = 0.3d;
	public const double MaxPupilMm = 8d;

	public static double PupilToPosition(double pupilMm)
	{
		var normalized = GetNormalizedPosition(pupilMm);
		return normalized * 100d;
	}

	public static double GetNormalizedPosition(double pupilMm)
	{
		if (!double.IsFinite(pupilMm) || pupilMm <= 0)
		{
			return 1d;
		}

		var clamped = Math.Clamp(pupilMm, MinPupilMm, MaxPupilMm);
		var numerator = Math.Log(MaxPupilMm) - Math.Log(clamped);
		var denominator = Math.Log(MaxPupilMm) - Math.Log(MinPupilMm);

		if (denominator <= 0 || !double.IsFinite(denominator))
		{
			return 0d;
		}

		return Math.Clamp(numerator / denominator, 0d, 1d);
	}
}
