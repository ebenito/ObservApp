namespace ObservApp.Shared.Services;

using CosineKitty;

public sealed class HomeAstronomyService : IHomeAstronomyService
{
	public HomeAstronomySnapshot? CalculateSnapshot(double latitude, double longitude, double altitudeMeters, DateTime nowUtc)
	{
		try
		{
			var observer = new Observer(latitude, longitude, altitudeMeters / 1000.0);
			var today = new AstroTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 12, 0, 0);
			var nowTime = new AstroTime(nowUtc);

			var sunrise = RiseSetTime(observer, today, Body.Sun, Direction.Rise);
			var sunset = RiseSetTime(observer, today, Body.Sun, Direction.Set);

			TimeSpan? dayLength = null;
			if (sunrise.HasValue && sunset.HasValue)
			{
				dayLength = sunset.Value - sunrise.Value;
			}

			var astroTwilightEnd = TwilightTime(observer, today, -18.0, Direction.Set);
			var astroTwilightStart = TwilightTime(observer, today, -18.0, Direction.Rise);

			var nowLocal = nowUtc.ToLocalTime();
			var isAstroNight =
				(astroTwilightEnd.HasValue && nowLocal > astroTwilightEnd.Value.ToLocalTime()) ||
				(astroTwilightStart.HasValue && nowLocal < astroTwilightStart.Value.ToLocalTime());

			var eqSun = Astronomy.Equator(Body.Sun, nowTime, observer, EquatorEpoch.OfDate, Aberration.Corrected);
			var hzSun = Astronomy.Horizon(nowTime, observer, eqSun.ra, eqSun.dec, Refraction.Normal);
			var isDaytime = hzSun.altitude > 0;

			var moonrise = RiseSetTime(observer, today, Body.Moon, Direction.Rise);
			var moonset = RiseSetTime(observer, today, Body.Moon, Direction.Set);

			var moonPhaseAngle = Astronomy.MoonPhase(nowTime);
			var ilum = Astronomy.Illumination(Body.Moon, nowTime);
			var moonIllum = ilum.phase_fraction * 100.0;
			var moonAge = (moonPhaseAngle / 360.0) * 29.53058770576;

			var nextFullMoon = Astronomy.SearchMoonPhase(180, nowTime, 30).ToUtcDateTime();

			return new HomeAstronomySnapshot
			{
				Sunrise = sunrise,
				Sunset = sunset,
				DayLength = dayLength,
				AstroTwilightEnd = astroTwilightEnd,
				IsAstroNight = isAstroNight,
				IsDaytime = isDaytime,
				Moonrise = moonrise,
				Moonset = moonset,
				MoonPhaseAngle = moonPhaseAngle,
				MoonIllumination = moonIllum,
				MoonAge = moonAge,
				NextFullMoonUtc = nextFullMoon
			};
		}
		catch
		{
			return null;
		}
	}

	private static DateTime? RiseSetTime(Observer observer, AstroTime day, Body body, Direction direction)
	{
		try
		{
			var start = new AstroTime(day.ToUtcDateTime().Date);
			var result = Astronomy.SearchRiseSet(body, observer, direction, start, 1.0);
			return result?.ToUtcDateTime();
		}
		catch
		{
			return null;
		}
	}

	private static DateTime? TwilightTime(Observer observer, AstroTime day, double altitude, Direction direction)
	{
		try
		{
			var now = day.ToUtcDateTime();
			var start = new AstroTime(now.Year, now.Month, now.Day, direction == Direction.Rise ? 0 : 12, 0, 0);
			var result = Astronomy.SearchAltitude(Body.Sun, observer, direction, start, 1.0, altitude);
			return result?.ToUtcDateTime();
		}
		catch
		{
			return null;
		}
	}
}
