using CosineKitty;
using ObservApp.Shared.Services;

namespace ObservApp.Shared.Services;

public sealed class EclipseCalculatorService : IEclipseCalculatorService
{
    // ── Eclipses solares 2024-2030 ────────────────────────────────────────────
    // Datos de referencia: NASA Eclipse page. La hora es del máximo global (UTC). / Saros Series Catalog
    private static readonly List<EclipseDefinition> _catalog = new()
    {
        new("2024-04-08",
            new DateTime(2024, 4,  8, 18,17,0,DateTimeKind.Utc),
            EclipseType.Total,   "América del Norte",
            SarosNumber: 139, SarosMember: 31, SarosTotal: 71),

        new("2024-10-02",
            new DateTime(2024,10,  2, 18,46,0,DateTimeKind.Utc),
            EclipseType.Annular, "Pacífico Sur / Sudamérica",
            SarosNumber: 144, SarosMember: 23, SarosTotal: 70),

        new("2026-02-17",
            new DateTime(2026, 2, 17,12, 2,0,DateTimeKind.Utc),
            EclipseType.Annular, "Antártida",
            SarosNumber: 121, SarosMember: 79, SarosTotal: 82),

        new("2026-08-12",
            new DateTime(2026, 8, 12,17,47,0,DateTimeKind.Utc),
            EclipseType.Total,   "Ártico / Europa / África",
            SarosNumber: 126, SarosMember: 48, SarosTotal: 72),

        new("2027-02-06",
            new DateTime(2027, 2,  6,16, 0,0,DateTimeKind.Utc),
            EclipseType.Annular, "Sudamérica / Atlántico",
            SarosNumber: 131, SarosMember: 52, SarosTotal: 70),

        new("2027-08-02",
            new DateTime(2027, 8,  2,10, 7,0,DateTimeKind.Utc),
            EclipseType.Total,   "África / Europa / Asia",
            SarosNumber: 136, SarosMember: 38, SarosTotal: 71),

        new("2028-01-26",
            new DateTime(2028, 1, 26,15,54,0,DateTimeKind.Utc),
            EclipseType.Annular, "Sudamérica",
            SarosNumber: 141, SarosMember: 29, SarosTotal: 70),

        new("2028-07-22",
            new DateTime(2028, 7, 22, 2,56,0,DateTimeKind.Utc),
            EclipseType.Total,   "Australia / Asia",
            SarosNumber: 146, SarosMember: 14, SarosTotal: 76),

        new("2030-06-01",
            new DateTime(2030, 6,  1, 6,29,0,DateTimeKind.Utc),
            EclipseType.Annular, "Europa / Asia",
            SarosNumber: 128, SarosMember: 58, SarosTotal: 73),

        new("2030-11-25",
            new DateTime(2030,11, 25, 6,51,0,DateTimeKind.Utc),
            EclipseType.Total,   "Sudáfrica / Oceanía",
            SarosNumber: 133, SarosMember: 45, SarosTotal: 72),
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
                return new LocalEclipseResult(LocalEclipseType.None, 0, contacts, false, null);

            localType = solar.kind switch
            {
                EclipseKind.Total   => LocalEclipseType.Total,
                EclipseKind.Annular => LocalEclipseType.Annular,
                _                   => LocalEclipseType.Partial,
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

            // ── Calcular métricas ────────────────────────────────────
            var metrics = ComputeMetrics(solar, eclipse, observer);
            return new LocalEclipseResult(localType, maxCoverage, contacts, sunVisible, metrics);
        }
        catch
        {
            return new LocalEclipseResult(LocalEclipseType.None, 0, contacts, false, null);
        }
    }

    private static EclipseMetrics ComputeMetrics(
    LocalSolarEclipseInfo solar,
    EclipseDefinition eclipse,
    Observer observer)
    {
        // 1. ΔT — usando el momento del máximo
        double deltaTSeconds = 0;
        if (solar.peak.time != null)
        {
            var t = solar.peak.time!;
            deltaTSeconds = (t.tt - t.ut) * 86400.0;
        }

        // 2. Duración de la totalidad (C2 → C3)
        TimeSpan? totalityDuration = null;
        if (solar.total_begin.time != null && solar.total_end.time != null)
        {
            totalityDuration = solar.total_end.time!.ToUtcDateTime()
                             - solar.total_begin.time!.ToUtcDateTime();
        }

        // 3. Magnitud lineal y Gamma — calculados en el momento del pico global
        double magnitude = 0;
        double gamma = 0;

        if (solar.peak.time != null)
        {
            var t = solar.peak.time!;

            // Posiciones topocéntricas Sol y Luna
            var sunEq = Astronomy.Equator(Body.Sun, t, observer, EquatorEpoch.OfDate, Aberration.Corrected);
            var moonEq = Astronomy.Equator(Body.Moon, t, observer, EquatorEpoch.OfDate, Aberration.Corrected);

            // Separación angular topocéntrica (grados)
            double dRa = (sunEq.ra - moonEq.ra) * 15.0;
            double dDec = sunEq.dec - moonEq.dec;
            double cosDec = Math.Cos(sunEq.dec * Math.PI / 180.0);
            double separation = Math.Sqrt(dRa * dRa * cosDec * cosDec + dDec * dDec);

            // Radios angulares (grados)
            var moonVec = Astronomy.GeoVector(Body.Moon, t, Aberration.Corrected);
            double moonDistAU = moonVec.Length();
            const double moonRadiusKm = 1737.4;
            const double kmPerAU = 149_597_870.7;
            double rMoon = Math.Atan2(moonRadiusKm, moonDistAU * kmPerAU) * (180.0 / Math.PI);

            var sunVec = Astronomy.GeoVector(Body.Sun, t, Aberration.Corrected);
            double sunDistAU = sunVec.Length();
            const double sunRadiusKm = 695_700.0;
            double rSun = Math.Atan2(sunRadiusKm, sunDistAU * kmPerAU) * (180.0 / Math.PI);

            // Magnitud lineal: (rSun + rMoon - separation) / (2 * rSun)
            // > 1.0 para eclipse total (Luna cubre completamente el Sol)
            magnitude = (rSun + rMoon - separation) / (2.0 * rSun);
            magnitude = Math.Max(0, magnitude);

            // Gamma: mínima distancia del eje de la sombra al centro de la Tierra
            // Aproximación: usamos la separación en el pico / radio solar en unidades de radio terrestre
            // Fórmula estándar simplificada basada en geometría de Bessel
            // gamma ≈ separación_geocéntrica_en_radios_terrestres
            // Para una aproximación razonable usamos la declinación del punto subsolar y el observador
            // Como aproximación práctica: gamma = sin(dec_luna) / sin(inclinación_lunar)
            // Usamos el método directo: distancia del eje umbra al centro de la Tierra
            var moonGeo = Astronomy.Equator(Body.Moon, t,
                new Observer(0, 0, 0), EquatorEpoch.OfDate, Aberration.Corrected);
            var sunGeo = Astronomy.Equator(Body.Sun, t,
                new Observer(0, 0, 0), EquatorEpoch.OfDate, Aberration.Corrected);

            // Separación geocéntrica Sol-Luna en grados
            double dRaGeo = (sunGeo.ra - moonGeo.ra) * 15.0;
            double dDecGeo = sunGeo.dec - moonGeo.dec;
            double cosDecGeo = Math.Cos(sunGeo.dec * Math.PI / 180.0);
            double sepGeo = Math.Sqrt(dRaGeo * dRaGeo * cosDecGeo * cosDecGeo + dDecGeo * dDecGeo);

            // Gamma en radios terrestres (radio terrestre ecuatorial ≈ 6378.1 km)
            // Proyección sobre plano fundamental de Bessel (simplificada)
            // sepGeo en grados → convertir a unidades de radio terrestre
            // 1 radio terrestre ≈ 0.00426 AU
            const double earthRadiusAU = 6378.1 / 149_597_870.7;
            double moonDistAUGeo = moonVec.Length();
            // Distancia del eje de la sombra al centro de la Tierra en km
            double sepRadians = sepGeo * Math.PI / 180.0;
            double shadowAxisDistKm = Math.Sin(sepRadians) * moonDistAU * kmPerAU;
            gamma = shadowAxisDistKm / 6378.1; // en radios terrestres
        }

        return new EclipseMetrics(
            DeltaTSeconds: deltaTSeconds,
            Magnitude: magnitude,
            Gamma: gamma,
            TotalityDuration: totalityDuration,
            SarosNumber: eclipse.SarosNumber,
            SarosMember: eclipse.SarosMember,
            SarosTotal: eclipse.SarosTotal);
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
            // ✅ Posiciones topocéntricas (desde el observador, no desde el centro de la Tierra)
            // Esto es lo que hace que el % varíe según ubicación y sea correcto en eclipses parciales
            var sunEq = Astronomy.Equator(Body.Sun, time,
                observer, EquatorEpoch.OfDate, Aberration.Corrected);
            var moonEq = Astronomy.Equator(Body.Moon, time,
                observer, EquatorEpoch.OfDate, Aberration.Corrected);

            // Separación angular topocéntrica Sol-Luna en grados
            double dRa = (sunEq.ra - moonEq.ra) * 15.0; // AR en horas → grados
            double dDec = sunEq.dec - moonEq.dec;
            // Corrección por la proyección en declinación
            double cosDec = Math.Cos(sunEq.dec * Math.PI / 180.0);
            double separation = Math.Sqrt(dRa * dRa * cosDec * cosDec + dDec * dDec);

            // Radios angulares medios (grados)
            // Sol: ~0.267°, Luna: varía entre 0.245° y 0.278° según distancia
            // Usamos la distancia real de la Luna para mayor precisión
            var moonVec = Astronomy.GeoVector(Body.Moon, time, Aberration.Corrected);
            double moonDistAU = moonVec.Length();
            // Radio lunar: 1737.4 km, 1 AU = 149 597 870.7 km
            const double moonRadiusKm = 1737.4;
            const double kmPerAU = 149_597_870.7;
            double rMoon = Math.Atan2(moonRadiusKm, moonDistAU * kmPerAU) * (180.0 / Math.PI);

            var sunVec = Astronomy.GeoVector(Body.Sun, time, Aberration.Corrected);
            double sunDistAU = sunVec.Length();
            const double sunRadiusKm = 695_700.0;
            double rSun = Math.Atan2(sunRadiusKm, sunDistAU * kmPerAU) * (180.0 / Math.PI);

            // Sin solapamiento
            if (separation >= rSun + rMoon) return 0.0;
            // Luna cubre completamente el Sol (total o anular)
            if (separation <= Math.Abs(rSun - rMoon))
                return Math.Clamp((rMoon * rMoon) / (rSun * rSun), 0.0, 1.0);

            // Área de intersección de dos círculos (fórmula exacta)
            double d = separation;
            double r1 = rSun;
            double r2 = rMoon;
            double d2 = d * d, r12 = r1 * r1, r22 = r2 * r2;
            double a1 = r12 * Math.Acos(Math.Clamp((d2 + r12 - r22) / (2 * d * r1), -1, 1));
            double a2 = r22 * Math.Acos(Math.Clamp((d2 + r22 - r12) / (2 * d * r2), -1, 1));
            double a3 = 0.5 * Math.Sqrt(Math.Max(0,
                             (-d + r1 + r2) * (d + r1 - r2) *
                             (d - r1 + r2) * (d + r1 + r2)));
            double intersection = a1 + a2 - a3;

            return Math.Clamp(intersection / (Math.PI * r12), 0.0, 1.0);
        }
        catch { return 0.0; }
    }

}