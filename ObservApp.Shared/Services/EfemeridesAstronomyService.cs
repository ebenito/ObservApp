namespace ObservApp.Shared.Services;

using CosineKitty;

public sealed class EfemeridesAstronomyService : IEfemeridesAstronomyService
{
	private readonly IEclipseCalculatorService _eclipseCalculator;

	public EfemeridesAstronomyService(IEclipseCalculatorService eclipseCalculator)
	{
		_eclipseCalculator = eclipseCalculator;
	}

	public EphComputationResult Compute(DateTime date, double latitude, double longitude, double altitudeMeters)
	{
		var observer = new Observer(latitude, longitude, altitudeMeters / 1000.0);
		var dayTime = new AstroTime(date.Year, date.Month, date.Day, 12, 0, 0);

		return new EphComputationResult
		{
			Planets = ComputePlanets(date, observer, dayTime),
			Events = ComputeEvents(date),
			Tonight = ComputeTonight(date, observer, dayTime)
		};
	}

	public (EclipseDefinition? Eclipse, LocalEclipseResult? Result) FindNextVisibleEclipse(
		DateTime nowUtc,
		double latitude,
		double longitude,
		double altitudeMeters,
		int yearsAhead = 2)
	{
		try
		{
			var limit = nowUtc.AddYears(yearsAhead);
			var eclipses = _eclipseCalculator.GetUpcomingEclipses(yearsAhead);

			foreach (var eclipse in eclipses.Where(e => e.Date > nowUtc && e.Date <= limit))
			{
				var result = _eclipseCalculator.Calculate(eclipse, latitude, longitude, altitudeMeters);
				if (result is { IsVisible: true } && result.Type != LocalEclipseType.None)
				{
					return (eclipse, result);
				}
			}
		}
		catch
		{
		}

		return (null, null);
	}

	public (EphNightWindow? Window, List<EphDsoVisibility> Visible) ComputeDsoVisibility(
		DateTime date,
		double latitude,
		double longitude,
		double altitudeMeters,
		IEnumerable<EphDsoCoordinate> dsos,
		CancellationToken cancellationToken = default)
	{
		var observer = new Observer(latitude, longitude, altitudeMeters / 1000.0);
		var window = TryGetAstronomicalNightWindow(date, observer);
		if (window is null)
		{
			return (null, new List<EphDsoVisibility>());
		}

		var visible = new List<EphDsoVisibility>();
		var bright = dsos.Where(d => d.Magnitude < 7.0).OrderBy(d => d.Magnitude);
		var faint = dsos.Where(d => d.Magnitude >= 7.0).OrderBy(d => d.Magnitude);

		ProcessDsoGroup(bright, observer, window, visible, cancellationToken);
		ProcessDsoGroup(faint, observer, window, visible, cancellationToken);

		visible.Sort((a, b) => b.MaxAltitude.CompareTo(a.MaxAltitude));
		return (window, visible);
	}

	private static List<EphPlanetData> ComputePlanets(DateTime date, Observer observer, AstroTime dayTime)
	{
		var definitions = new[]
		{
			(Body.Mercury, "Mercurio", "☿"),
			(Body.Venus,   "Venus",    "♀"),
			(Body.Mars,    "Marte",    "♂"),
			(Body.Jupiter, "Júpiter",  "♃"),
			(Body.Saturn,  "Saturno",  "♄"),
			(Body.Uranus,  "Urano",    "⛢"),
			(Body.Neptune, "Neptuno",  "♆"),
		};

		var result = new List<EphPlanetData>();

		foreach (var (body, name, emoji) in definitions)
		{
			try
			{
				double maxAlt = double.MinValue;
				double azAtMax = 0;

				for (var hour = 0; hour < 24; hour++)
				{
					var time = new AstroTime(date.Year, date.Month, date.Day, hour, 0, 0);
					var eq = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
					var hz = Astronomy.Horizon(time, observer, eq.ra, eq.dec, Refraction.Normal);
					if (hz.altitude > maxAlt)
					{
						maxAlt = hz.altitude;
						azAtMax = hz.azimuth;
					}
				}

				DateTime? rise = null;
				DateTime? set = null;

				try
				{
					var startDay = new AstroTime(date.Year, date.Month, date.Day, 0, 0, 0);
					rise = Astronomy.SearchRiseSet(body, observer, Direction.Rise, startDay, 1.0)?.ToUtcDateTime();
				}
				catch
				{
				}

				try
				{
					var startDay = new AstroTime(date.Year, date.Month, date.Day, 0, 0, 0);
					set = Astronomy.SearchRiseSet(body, observer, Direction.Set, startDay, 1.0)?.ToUtcDateTime();
				}
				catch
				{
				}

				var distanceAu = 0.0;
				try
				{
					var vec = Astronomy.GeoVector(body, dayTime, Aberration.Corrected);
					distanceAu = Math.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
				}
				catch
				{
				}

				var illumination = 0.0;
				try
				{
					illumination = Astronomy.Illumination(body, dayTime).phase_fraction * 100.0;
				}
				catch
				{
				}

				result.Add(new EphPlanetData
				{
					Name = name,
					Emoji = emoji,
					Body = body,
					IsVisible = maxAlt > 0,
					MaxAltitude = maxAlt,
					AzimuthAtMax = azAtMax,
					RiseTime = rise,
					SetTime = set,
					DistanceAU = distanceAu,
					IlluminationPct = illumination
				});
			}
			catch
			{
			}
		}

		result.Sort((a, b) => b.MaxAltitude.CompareTo(a.MaxAltitude));
		return result;
	}

	private static List<EphEventData> ComputeEvents(DateTime date)
	{
		var events = new List<EphEventData>();
		var limit30 = date.AddDays(30);
		var limit90 = date.AddDays(90);

		var moonPhases = new[]
		{
			(0.0,   "🌑", "Luna Nueva"),
			(90.0,  "🌓", "Cuarto Creciente"),
			(180.0, "🌕", "Luna Llena"),
			(270.0, "🌗", "Cuarto Menguante"),
		};

		foreach (var (angle, emoji, name) in moonPhases)
		{
			try
			{
				var start = new AstroTime(date);
				for (var i = 0; i < 2; i++)
				{
					var phase = Astronomy.SearchMoonPhase(angle, start, 35);
					var dt = phase.ToUtcDateTime().Date;
					if (dt >= date.Date && dt <= limit30.Date)
					{
						events.Add(new EphEventData { Emoji = emoji, Date = dt, Description = name });
					}

					start = new AstroTime(dt.AddDays(25));
					if (dt > limit30)
					{
						break;
					}
				}
			}
			catch
			{
			}
		}

		for (var year = date.Year; year <= date.Year + 1; year++)
		{
			try
			{
				var seasons = Astronomy.Seasons(year);
				var seasonDefs = new[]
				{
					(seasons.mar_equinox.ToUtcDateTime(), "🌸", "Equinoccio de primavera"),
					(seasons.jun_solstice.ToUtcDateTime(), "☀️", "Solsticio de verano"),
					(seasons.sep_equinox.ToUtcDateTime(), "🍂", "Equinoccio de otoño"),
					(seasons.dec_solstice.ToUtcDateTime(), "❄️", "Solsticio de invierno"),
				};

				foreach (var (dt, emoji, desc) in seasonDefs)
				{
					if (dt.Date >= date.Date && dt.Date <= limit90.Date)
					{
						events.Add(new EphEventData { Emoji = emoji, Date = dt.Date, Description = desc });
					}
				}
			}
			catch
			{
			}
		}

		events.Sort((a, b) => a.Date.CompareTo(b.Date));
		return events;
	}

	private static EphTonightData? ComputeTonight(DateTime date, Observer observer, AstroTime dayTime)
	{
		try
		{
			var ilum = Astronomy.Illumination(Body.Moon, dayTime);
			var moonIllum = ilum.phase_fraction * 100.0;
			var moonPhaseAngle = Astronomy.MoonPhase(dayTime);

			DateTime? astroNightStart = null;
			try
			{
				astroNightStart = Astronomy
					.SearchAltitude(Body.Sun, observer, Direction.Set, dayTime, 1.0, -18.0)
					?.ToUtcDateTime();
			}
			catch
			{
			}

			DateTime? moonRise = null;
			DateTime? moonSet = null;

			try
			{
				var startDay = new AstroTime(date.Year, date.Month, date.Day, 0, 0, 0);
				moonRise = Astronomy.SearchRiseSet(Body.Moon, observer, Direction.Rise, startDay, 1.0)?.ToUtcDateTime();
			}
			catch
			{
			}

			try
			{
				var startDay = new AstroTime(date.Year, date.Month, date.Day, 0, 0, 0);
				moonSet = Astronomy.SearchRiseSet(Body.Moon, observer, Direction.Set, startDay, 1.0)?.ToUtcDateTime();
			}
			catch
			{
			}

			var quality = moonIllum < 25
				? EphNightQuality.Good
				: moonIllum < 65
					? EphNightQuality.Fair
					: EphNightQuality.Poor;

			return new EphTonightData
			{
				MoonIlluminationPct = moonIllum,
				MoonPhaseAngle = moonPhaseAngle,
				AstroNightStart = astroNightStart,
				MoonRise = moonRise,
				MoonSet = moonSet,
				Quality = quality
			};
		}
		catch
		{
			return null;
		}
	}

	private static void ProcessDsoGroup(
		IEnumerable<EphDsoCoordinate> dsos,
		Observer observer,
		EphNightWindow window,
		ICollection<EphDsoVisibility> visible,
		CancellationToken cancellationToken)
	{
		foreach (var dso in dsos)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var result = CalculateDsoVisibility(dso, observer, window);
			if (result is { IsVisibleTonight: true })
			{
				visible.Add(result);
			}
		}
	}

	private static EphDsoVisibility? CalculateDsoVisibility(EphDsoCoordinate dso, Observer observer, EphNightWindow window)
	{
		const double minAltitude = 10.0;
		const int stepMinutes = 15;

		var maxAlt = double.MinValue;
		var azAtMax = 0.0;
		DateTime? bestTime = null;
		DateTime? visibleFrom = null;
		DateTime? visibleUntil = null;

		for (var sample = window.StartUtc; sample <= window.EndUtc; sample = sample.AddMinutes(stepMinutes))
		{
			var (altitude, azimuth) = GetDsoPosition(dso.RaDeg, dso.DecDeg, observer, new AstroTime(sample));
			if (altitude > maxAlt)
			{
				maxAlt = altitude;
				azAtMax = azimuth;
				bestTime = sample;
			}

			if (altitude > minAltitude)
			{
				visibleFrom ??= sample;
				visibleUntil = sample;
			}
		}

		if (!visibleFrom.HasValue || !visibleUntil.HasValue || maxAlt <= minAltitude)
		{
			return null;
		}

		var totalMinutes = Math.Max(1.0, (window.EndUtc - window.StartUtc).TotalMinutes);
		var startPercent = Math.Clamp((visibleFrom.Value - window.StartUtc).TotalMinutes / totalMinutes * 100.0, 0.0, 100.0);
		var endPercent = Math.Clamp((visibleUntil.Value - window.StartUtc).TotalMinutes / totalMinutes * 100.0, 0.0, 100.0);

		return new EphDsoVisibility
		{
			Id = dso.Id,
			IsVisibleTonight = true,
			MaxAltitude = maxAlt,
			AzimuthAtMax = azAtMax,
			VisibleFrom = visibleFrom,
			VisibleUntil = visibleUntil,
			BestViewingTime = bestTime,
			VisibleStartPercent = startPercent,
			VisibleWidthPercent = Math.Max(1.5, endPercent - startPercent)
		};
	}

	private static EphNightWindow? TryGetAstronomicalNightWindow(DateTime date, Observer observer)
	{
		try
		{
			var localNoon = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Local);
			var duskSearch = new AstroTime(localNoon.ToUniversalTime());
			var dusk = Astronomy.SearchAltitude(Body.Sun, observer, Direction.Set, duskSearch, 1.0, -18.0);
			if (dusk is null)
			{
				return null;
			}

			var dawn = Astronomy.SearchAltitude(Body.Sun, observer, Direction.Rise, dusk, 1.0, -18.0);
			if (dawn is null)
			{
				return null;
			}

			var duskUtc = dusk.ToUtcDateTime();
			var dawnUtc = dawn.ToUtcDateTime();
			if (dawnUtc <= duskUtc)
			{
				return null;
			}

			return new EphNightWindow { StartUtc = duskUtc, EndUtc = dawnUtc };
		}
		catch
		{
			return null;
		}
	}

	private static (double Altitude, double Azimuth) GetDsoPosition(double raDeg, double decDeg, Observer observer, AstroTime time)
	{
		var raHours = raDeg / 15.0;
		var hz = Astronomy.Horizon(time, observer, raHours, decDeg, Refraction.Normal);
		return (hz.altitude, hz.azimuth);
	}
}
