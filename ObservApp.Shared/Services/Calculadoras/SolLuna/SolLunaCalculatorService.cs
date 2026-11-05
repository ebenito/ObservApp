namespace ObservApp.Shared.Services;

using CosineKitty;

public sealed class SolLunaCalculatorService : ISolLunaCalculatorService
{
	private const double SynodicMonthDays = 29.53058770576;
	private const double KmPerAu = 149_597_870.7;
	private const double MoonRadiusKm = 1737.4;

	public SolLunaResult Calculate(DateTime date, double latitude, double longitude, double altitudeMeters)
	{
		var observer = new Observer(latitude, longitude, altitudeMeters);
		var noon = new AstroTime(date.Year, date.Month, date.Day, 12, 0, 0);

		return new SolLunaResult(
			ComputeSun(date, observer, noon),
			ComputeMoon(date, observer, noon),
			ComputeYearEvents(date.Year, observer));
	}

	public MoonPhaseKind ClassifyPhase(double phaseAngleDeg) => phaseAngleDeg switch
	{
		< 22.5 => MoonPhaseKind.New,
		< 67.5 => MoonPhaseKind.WaxingCrescent,
		< 112.5 => MoonPhaseKind.FirstQuarter,
		< 157.5 => MoonPhaseKind.WaxingGibbous,
		< 202.5 => MoonPhaseKind.Full,
		< 247.5 => MoonPhaseKind.WaningGibbous,
		< 292.5 => MoonPhaseKind.LastQuarter,
		< 337.5 => MoonPhaseKind.WaningCrescent,
		_ => MoonPhaseKind.New // 337.5–360 cierra el ciclo
	};

	// ── Sol ──────────────────────────────────────────────────────────────────
	private static SolLunaSunData ComputeSun(DateTime date, Observer observer, AstroTime noon)
	{
		var sunrise = RiseSetTime(observer, noon, Body.Sun, Direction.Rise);
		var transit = TransitTime(observer, noon, Body.Sun);
		var sunset = RiseSetTime(observer, noon, Body.Sun, Direction.Set);

		var maxAltitude = double.NaN;
		var transitAzimuth = double.NaN;
		if (transit.HasValue)
		{
			var hor = HorizontalAt(Body.Sun, new AstroTime(transit.Value), observer);
			maxAltitude = hor.altitude;
			transitAzimuth = hor.azimuth;
		}

		var eqNoon = Astronomy.Equator(Body.Sun, noon, observer, EquatorEpoch.OfDate, Aberration.Corrected);

		return new SolLunaSunData
		{
			Sunrise = sunrise,
			Transit = transit,
			Sunset = sunset,
			CivilDawn = TwilightTime(observer, date, -6.0, Direction.Rise),
			NauticalDawn = TwilightTime(observer, date, -12.0, Direction.Rise),
			AstronomicalDawn = TwilightTime(observer, date, -18.0, Direction.Rise),
			CivilDusk = TwilightTime(observer, date, -6.0, Direction.Set),
			NauticalDusk = TwilightTime(observer, date, -12.0, Direction.Set),
			AstronomicalDusk = TwilightTime(observer, date, -18.0, Direction.Set),
			DayLength = sunrise.HasValue && sunset.HasValue ? sunset.Value - sunrise.Value : null,
			RiseAzimuthDeg = HorizonAzimuth(observer, sunrise, Body.Sun),
			SetAzimuthDeg = HorizonAzimuth(observer, sunset, Body.Sun),
			TransitAzimuthDeg = transitAzimuth,
			MaxAltitudeDeg = maxAltitude,
			DeclinationDeg = eqNoon.dec,
			RightAscensionHours = eqNoon.ra,
			EquationOfTimeMinutes = EquationOfTimeMinutes(noon),
			DistanceAu = Astronomy.HelioVector(Body.Earth, noon).Length(),
			Path = ComputePath(Body.Sun, date, observer)
		};
	}

	// ── Luna ─────────────────────────────────────────────────────────────────
	private SolLunaMoonData ComputeMoon(DateTime date, Observer observer, AstroTime noon)
	{
		var moonrise = RiseSetTime(observer, noon, Body.Moon, Direction.Rise);
		var transit = TransitTime(observer, noon, Body.Moon);
		var moonset = RiseSetTime(observer, noon, Body.Moon, Direction.Set);

		var maxAltitude = double.NaN;
		if (transit.HasValue)
		{
			maxAltitude = HorizontalAt(Body.Moon, new AstroTime(transit.Value), observer).altitude;
		}

		var eqNoon = Astronomy.Equator(Body.Moon, noon, observer, EquatorEpoch.OfDate, Aberration.Corrected);

		var distanceKm = Astronomy.GeoVector(Body.Moon, noon, Aberration.Corrected).Length() * KmPerAu;
		var phaseAngle = Astronomy.MoonPhase(noon); // 0 = nueva … 180 = llena

		return new SolLunaMoonData
		{
			Moonrise = moonrise,
			Transit = transit,
			Moonset = moonset,
			RiseAzimuthDeg = HorizonAzimuth(observer, moonrise, Body.Moon),
			SetAzimuthDeg = HorizonAzimuth(observer, moonset, Body.Moon),
			MaxAltitudeDeg = maxAltitude,
			DeclinationDeg = eqNoon.dec,
			DistanceKm = distanceKm,
			AngularDiameterDeg = 2.0 * Math.Atan2(MoonRadiusKm, distanceKm) * 180.0 / Math.PI,
			IlluminationPercent = Astronomy.Illumination(Body.Moon, noon).phase_fraction * 100.0,
			PhaseAngleDeg = phaseAngle,
			AgeDays = phaseAngle / 360.0 * SynodicMonthDays,
			Phase = ClassifyPhase(phaseAngle),
			NextNewMoonUtc = Astronomy.SearchMoonPhase(0, noon, 30).ToUtcDateTime(),
			NextFullMoonUtc = Astronomy.SearchMoonPhase(180, noon, 30).ToUtcDateTime(),
			Path = ComputePath(Body.Moon, date, observer)
		};
	}

	// ── Eventos del año ──────────────────────────────────────────────────────
	private static SolLunaYearEvents ComputeYearEvents(int year, Observer observer)
	{
		var seasons = Astronomy.Seasons(year);

		return new SolLunaYearEvents
		{
			MarchEquinoxUtc = seasons.mar_equinox.ToUtcDateTime(),
			JuneSolsticeUtc = seasons.jun_solstice.ToUtcDateTime(),
			SeptemberEquinoxUtc = seasons.sep_equinox.ToUtcDateTime(),
			DecemberSolsticeUtc = seasons.dec_solstice.ToUtcDateTime(),
			MoonPhases = ComputeMoonPhases(year),
			Eclipses = ComputeLocalEclipses(year, observer)
		};
	}

	private static List<SolLunaMoonPhaseEvent> ComputeMoonPhases(int year)
	{
		var phases = new List<SolLunaMoonPhaseEvent>();
		var start = new AstroTime(year, 1, 1, 0, 0, 0);

		foreach (var (angle, kind) in new[]
		{
			(0.0, MoonPhaseKind.New),
			(90.0, MoonPhaseKind.FirstQuarter),
			(180.0, MoonPhaseKind.Full),
			(270.0, MoonPhaseKind.LastQuarter)
		})
		{
			var t = start;
			// Como mucho 14 lunaciones por año
			for (var i = 0; i < 14; i++)
			{
				var found = Astronomy.SearchMoonPhase(angle, t, 35);
				if (found is null)
				{
					break;
				}

				var utc = found.ToUtcDateTime();
				if (utc.Year != year)
				{
					break;
				}

				phases.Add(new SolLunaMoonPhaseEvent(kind, utc));
				t = new AstroTime(utc.AddDays(25));
			}
		}

		phases.Sort((a, b) => a.UtcTime.CompareTo(b.UtcTime));
		return phases;
	}

	private static List<SolLunaEclipseEvent> ComputeLocalEclipses(int year, Observer observer)
	{
		var eclipses = new List<SolLunaEclipseEvent>();
		var start = new AstroTime(year, 1, 1, 0, 0, 0);

		// Eclipses solares visibles desde la ubicación
		var tSolar = start;
		for (var i = 0; i < 12; i++)
		{
			try
			{
				var global = Astronomy.SearchGlobalSolarEclipse(tSolar);
				var globalPeak = global.peak.ToUtcDateTime();
				if (globalPeak.Year > year)
				{
					break;
				}

				var local = Astronomy.SearchLocalSolarEclipse(new AstroTime(globalPeak.AddDays(-1)), observer);
				if (local.kind != EclipseKind.None &&
					Math.Abs((local.peak.time.ToUtcDateTime() - globalPeak).TotalHours) < 48)
				{
					eclipses.Add(new SolLunaEclipseEvent(
						SolLunaEclipseBody.Solar,
						MapKind(local.kind),
						local.peak.time.ToUtcDateTime(),
						local.obscuration,
						local.partial_begin.time?.ToUtcDateTime(),
						local.total_begin.time?.ToUtcDateTime(),
						local.total_end.time?.ToUtcDateTime(),
						local.partial_end.time?.ToUtcDateTime()));
				}

				tSolar = new AstroTime(globalPeak.AddDays(30));
			}
			catch
			{
				break;
			}
		}

		// Eclipses lunares: visibles si la Luna está sobre el horizonte en el máximo
		var tLunar = start;
		for (var i = 0; i < 12; i++)
		{
			try
			{
				var global = Astronomy.SearchLunarEclipse(tLunar);
				var peak = global.peak.ToUtcDateTime();
				if (peak.Year > year)
				{
					break;
				}

				if (HorizontalAt(Body.Moon, global.peak, observer).altitude > -1.0)
				{
					// Sin contactos locales: los globales confundirían más que ayudarían
					eclipses.Add(new SolLunaEclipseEvent(
						SolLunaEclipseBody.Lunar,
						MapKind(global.kind),
						peak,
						global.obscuration,
						null, null, null, null));
				}

				tLunar = new AstroTime(peak.AddDays(30));
			}
			catch
			{
				break;
			}
		}

		eclipses.Sort((a, b) => a.PeakUtc.CompareTo(b.PeakUtc));
		return eclipses;
	}

	private static SolLunaEclipseKind MapKind(EclipseKind kind) => kind switch
	{
		EclipseKind.Total => SolLunaEclipseKind.Total,
		EclipseKind.Annular => SolLunaEclipseKind.Annular,
		EclipseKind.Penumbral => SolLunaEclipseKind.Penumbral,
		_ => SolLunaEclipseKind.Partial
	};

	// ── Helpers AstronomyEngine ──────────────────────────────────────────────
	private static List<SolLunaPathPoint> ComputePath(Body body, DateTime date, Observer observer)
	{
		var path = new List<SolLunaPathPoint>(24);
		for (var h = 0; h < 24; h++)
		{
			var hor = HorizontalAt(body, new AstroTime(date.Year, date.Month, date.Day, h, 0, 0), observer);
			path.Add(new SolLunaPathPoint(h, hor.altitude, hor.azimuth));
		}

		return path;
	}

	private static Topocentric HorizontalAt(Body body, AstroTime time, Observer observer)
	{
		var eq = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
		return Astronomy.Horizon(time, observer, eq.ra, eq.dec, Refraction.Normal);
	}

	private static DateTime? RiseSetTime(Observer observer, AstroTime day, Body body, Direction direction)
	{
		try
		{
			var startOfDay = new AstroTime(day.ToUtcDateTime().Date);
			return Astronomy.SearchRiseSet(body, observer, direction, startOfDay, 1.0)?.ToUtcDateTime();
		}
		catch
		{
			return null;
		}
	}

	private static DateTime? TransitTime(Observer observer, AstroTime day, Body body)
	{
		try
		{
			var startOfDay = new AstroTime(day.ToUtcDateTime().Date);
			return Astronomy.SearchHourAngle(body, observer, 0.0, startOfDay, 1).time.ToUtcDateTime();
		}
		catch
		{
			return null;
		}
	}

	private static double HorizonAzimuth(Observer observer, DateTime? eventUtc, Body body)
	{
		if (!eventUtc.HasValue)
		{
			return double.NaN;
		}

		try
		{
			return HorizontalAt(body, new AstroTime(eventUtc.Value), observer).azimuth;
		}
		catch
		{
			return double.NaN;
		}
	}

	private static DateTime? TwilightTime(Observer observer, DateTime date, double altitude, Direction direction)
	{
		// Amanecer: se busca desde las 00:00 UTC; anochecer: desde las 12:00 UTC
		try
		{
			var start = new AstroTime(date.Year, date.Month, date.Day, direction == Direction.Rise ? 0 : 12, 0, 0);
			return Astronomy.SearchAltitude(Body.Sun, observer, direction, start, 1.0, altitude)?.ToUtcDateTime();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Ecuación del tiempo a la hora dada: ángulo horario del Sol verdadero en Greenwich
	/// (GAST − AR) menos el del Sol medio, en minutos. Positiva cuando el reloj de sol adelanta.
	/// </summary>
	private static double EquationOfTimeMinutes(AstroTime time)
	{
		try
		{
			var eq = Astronomy.Equator(Body.Sun, time, new Observer(0, 0, 0), EquatorEpoch.OfDate, Aberration.Corrected);
			var trueHourAngle = Astronomy.SiderealTime(time) - eq.ra;
			var meanHourAngle = time.ToUtcDateTime().TimeOfDay.TotalHours - 12.0;
			var diffHours = trueHourAngle - meanHourAngle;

			// Normalizar a [-12, 12) h
			diffHours = ((diffHours + 12.0) % 24.0 + 24.0) % 24.0 - 12.0;
			return diffHours * 60.0;
		}
		catch
		{
			return double.NaN;
		}
	}
}
