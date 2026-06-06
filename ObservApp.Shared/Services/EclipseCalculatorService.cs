using CosineKitty;
using ObservApp.Shared.Services;

namespace ObservApp.Shared.Services;

public sealed class EclipseCalculatorService : IEclipseCalculatorService
{
    // ── Eclipses solares 2024-2030 ────────────────────────────────────────────
    // Datos de referencia: NASA Eclipse page. La hora es del máximo global (UTC).
    private static readonly List<EclipseDefinition> _catalog = new()
    {
        new("2024-04-08", new DateTime(2024, 4,  8, 18,17,0,DateTimeKind.Utc),
            EclipseType.Total,   "América del Norte"),
        new("2024-10-02", new DateTime(2024,10,  2, 18,46,0,DateTimeKind.Utc),
            EclipseType.Annular, "Pacífico Sur / Sudamérica"),
        new("2026-02-17", new DateTime(2026, 2, 17,12, 2,0,DateTimeKind.Utc),
            EclipseType.Annular, "Antártida"),
        new("2026-08-12", new DateTime(2026, 8, 12,17,47,0,DateTimeKind.Utc),
            EclipseType.Total,   "Ártico / Europa / África"),
        new("2027-02-06", new DateTime(2027, 2,  6,16, 0,0,DateTimeKind.Utc),
            EclipseType.Annular, "Sudamérica / Atlántico"),
        new("2027-08-02", new DateTime(2027, 8,  2,10,7, 0,DateTimeKind.Utc),
            EclipseType.Total,   "África / Europa / Asia"),
        new("2028-01-26", new DateTime(2028, 1, 26,15,54,0,DateTimeKind.Utc),
            EclipseType.Annular, "Sudamérica"),
        new("2028-07-22", new DateTime(2028, 7, 22, 2,56,0,DateTimeKind.Utc),
            EclipseType.Total,   "Australia / Asia"),
        new("2030-06-01", new DateTime(2030, 6,  1, 6,29,0,DateTimeKind.Utc),
            EclipseType.Annular, "Europa / Asia"),
        new("2030-11-25", new DateTime(2030,11, 25, 6,51,0,DateTimeKind.Utc),
            EclipseType.Total,   "Sudáfrica / Oceanía"),
    };

    public IReadOnlyList<EclipseDefinition> GetUpcomingEclipses(int years = 6)
    {
        var cutoff = DateTime.UtcNow.AddYears(years);
        return _catalog
            .Where(e => e.Date >= DateTime.UtcNow.AddDays(-1) && e.Date <= cutoff)
            .OrderBy(e => e.Date)
            .ToList();
    }

    public LocalEclipseResult? Calculate(
        EclipseDefinition eclipse,
        double latitudeDeg,
        double longitudeDeg,
        double altitudeMeters)
    {
        var observer = new Observer(latitudeDeg, longitudeDeg, altitudeMeters / 1000.0);
        var startTime = new AstroTime(eclipse.Date.AddHours(-4));
        var endTime = new AstroTime(eclipse.Date.AddHours(4));

        // Calcular la ocultación máxima local y los 4 contactos
        var contacts = new List<EclipseContact>();
        var maxCoverage = 0.0;
        LocalEclipseType localType;

        try
        {
            // Buscar el eclipse solar local
            var solar = Astronomy.SearchLocalSolarEclipse(startTime, observer);

            if (solar.kind == EclipseKind.None)
                return new LocalEclipseResult(LocalEclipseType.None, 0, contacts, false);

            localType = solar.kind switch
            {
                EclipseKind.Total => LocalEclipseType.Total,
                EclipseKind.Annular => LocalEclipseType.Annular,
                _ => LocalEclipseType.Partial,
            };

            // C1 — primer contacto externo
            if (solar.partial_begin.time != null)
                contacts.Add(BuildContact(EclipseEventKind.C1,
                    solar.partial_begin.time!, observer, 0.0));

            // C2 — inicio de totalidad/anularidad
            if (solar.total_begin.time != null)
            {
                // Baily's Beads: ~30s antes de C2
                var bailyStartT = new AstroTime(
                    solar.total_begin.time!.ToUtcDateTime().AddSeconds(-30));
                contacts.Add(BuildContact(EclipseEventKind.BailyBeadsStart,
                    bailyStartT, observer, 0.99));

                // Anillo de diamante: ~5s antes de C2
                var drStartT = new AstroTime(
                    solar.total_begin.time!.ToUtcDateTime().AddSeconds(-5));
                contacts.Add(BuildContact(EclipseEventKind.DiamondRingStart,
                    drStartT, observer, 0.999));

                contacts.Add(BuildContact(EclipseEventKind.C2,
                    solar.total_begin.time!, observer, 1.0));
            }

            // Máximo
            if (solar.peak.time != null)
            {
                maxCoverage = ComputeCoverage(solar.peak.time!, observer);
                contacts.Add(BuildContact(EclipseEventKind.Maximum,
                    solar.peak.time!, observer, maxCoverage));
            }

            // C3 — fin de totalidad/anularidad
            if (solar.total_end.time != null)
            {
                contacts.Add(BuildContact(EclipseEventKind.C3,
                    solar.total_end.time!, observer, 1.0));

                // Anillo de diamante: ~5s después de C3
                var drEndT = new AstroTime(
                    solar.total_end.time!.ToUtcDateTime().AddSeconds(5));
                contacts.Add(BuildContact(EclipseEventKind.DiamondRingEnd,
                    drEndT, observer, 0.999));

                // Baily's Beads: ~30s después de C3
                var bailyEndT = new AstroTime(
                    solar.total_end.time!.ToUtcDateTime().AddSeconds(30));
                contacts.Add(BuildContact(EclipseEventKind.BailyBeadsEnd,
                    bailyEndT, observer, 0.99));
            }

            // C4 — fin de eclipse parcial
            if (solar.partial_end.time != null)
                contacts.Add(BuildContact(EclipseEventKind.C4,
                    solar.partial_end.time!, observer, 0.0));

            // Bandas de sombra: unos 60s antes de C2 (solo si es total)
            if (localType == LocalEclipseType.Total && solar.total_begin.time != null)
            {
                var shadowT = new AstroTime(
                    solar.total_begin.time!.ToUtcDateTime().AddSeconds(-60));
                contacts.Insert(contacts.FindIndex(
                    c => c.Kind == EclipseEventKind.BailyBeadsStart),
                    BuildContact(EclipseEventKind.ShadowBands, shadowT, observer, 0.98));
            }

            // Verificar si el Sol está sobre el horizonte en el máximo
            bool sunVisible = true;
            if (solar.peak.time != null)
            {
                var eqc = Astronomy.Equator(Body.Sun, solar.peak.time!,
                    observer, EquatorEpoch.OfDate, Aberration.Corrected);
                var hz = Astronomy.Horizon(solar.peak.time!, observer,
                    eqc.ra, eqc.dec, Refraction.Normal);
                sunVisible = hz.altitude > 0;
            }

            contacts = contacts.OrderBy(c => c.UtcTime).ToList();

            return new LocalEclipseResult(localType, maxCoverage, contacts, sunVisible);
        }
        catch
        {
            return new LocalEclipseResult(LocalEclipseType.None, 0, contacts, false);
        }
    }

    private static EclipseContact BuildContact(
        EclipseEventKind kind,
        AstroTime time,
        Observer observer,
        double coverage)
    {
        var utc = time.ToUtcDateTime();
        var eqc = Astronomy.Equator(Body.Sun, time,
            observer, EquatorEpoch.OfDate, Aberration.Corrected);
        var hz = Astronomy.Horizon(time, observer,
            eqc.ra, eqc.dec, Refraction.Normal);

        return new EclipseContact(
            kind, utc, string.Empty,   // Label se pone en la página
            hz.altitude, hz.azimuth, coverage);
    }

    private static double ComputeCoverage(AstroTime time, Observer observer)
    {
        try
        {
            var illum = Astronomy.Illumination(Body.Moon, time);
            // Aproximación: la cobertura máxima se estima con la umbra
            // Un cálculo exacto requeriría la separación angular Sol-Luna
            // Para uso práctico usamos el ángulo de fase
            double separation = Astronomy.AngleBetween(
                Astronomy.GeoVector(Body.Sun, time, Aberration.Corrected),
                Astronomy.GeoVector(Body.Moon, time, Aberration.Corrected));

            // Radio angular del Sol ≈ 0.267°, de la Luna ≈ 0.259° (media)
            const double rSun = 0.267;
            const double rMoon = 0.259;

            if (separation >= rSun + rMoon) return 0.0;
            if (separation <= Math.Abs(rSun - rMoon)) return 1.0;

            // Área de intersección de dos círculos
            double d = separation;
            double r1 = rSun;
            double r2 = rMoon;
            double d2 = d * d;
            double r12 = r1 * r1;
            double r22 = r2 * r2;
            double a1 = r12 * Math.Acos((d2 + r12 - r22) / (2 * d * r1));
            double a2 = r22 * Math.Acos((d2 + r22 - r12) / (2 * d * r2));
            double a3 = 0.5 * Math.Sqrt((-d + r1 + r2) * (d + r1 - r2)
                                       * (d - r1 + r2) * (d + r1 + r2));
            double intersection = a1 + a2 - a3;
            return Math.Clamp(intersection / (Math.PI * r12), 0.0, 1.0);
        }
        catch { return 0.0; }
    }
}