namespace ObservApp.Shared.Services;

public record EclipseDefinition(
    string Id,
    DateTime Date,          // fecha UTC aproximada del máximo
    EclipseType MaxType,    // tipo máximo global
    string RegionLabel,      // "América del Norte", "Europa", etc.
    int SarosNumber,
    int SarosMember,
    int SarosTotal
);

public enum EclipseType { Total, Annular, Hybrid, Partial }

public enum LocalEclipseType { Total, Annular, Partial, None }

public record EclipseContact(
    EclipseEventKind Kind,
    DateTime UtcTime,
    string Label,           // ya localizado — se pasa desde la página
    double SunAltitudeDeg,
    double SunAzimuthDeg,
    double CoverageFraction // 0..1, fracción del disco solar cubierto
);

public enum EclipseEventKind
{
    C1, BailyBeadsStart, DiamondRingStart,
    Maximum, DiamondRingEnd, BailyBeadsEnd,
    C2, C3, C4, ShadowBands
}


/// <summary>
/// Métricas adicionales calculadas para el eclipse local.
/// </summary>
/// <param name="DeltaTSeconds">ΔT = TT − UT en segundos.</param>
/// <param name="Magnitude">Magnitud lineal del eclipse (fracción del diámetro solar cubierto). > 1 para totales.</param>
/// <param name="Gamma">Parámetro Gamma: distancia mínima eje sombra-centro Tierra en radios terrestres.</param>
/// <param name="TotalityDuration">Duración de la totalidad/anularidad (C2→C3). Null si es parcial.</param>
/// <param name="SarosNumber">Número de ciclo Saros.</param>
/// <param name="SarosMember">Miembro dentro del ciclo Saros (ej. 48).</param>
/// <param name="SarosTotal">Total de eclipses en el ciclo Saros (ej. 72).</param>
public record EclipseMetrics(
    double DeltaTSeconds,
    double Magnitude,
    double Gamma,
    TimeSpan? TotalityDuration,
    int SarosNumber,
    int SarosMember,
    int SarosTotal
);


public record LocalEclipseResult(
    LocalEclipseType Type,
    double MaxCoverage,         // 0..1
    List<EclipseContact> Contacts,
    bool IsVisible,              // false si el Sol está bajo el horizonte
    EclipseMetrics? Metrics
);

public interface IEclipseCalculatorService
{
    IReadOnlyList<EclipseDefinition> GetUpcomingEclipses(int years = 6);

    /// <summary>
    /// Calcula los contactos locales para la ubicación dada.
    /// Devuelve null si el eclipse no es visible desde esa ubicación.
    /// </summary>
    LocalEclipseResult? Calculate(
        EclipseDefinition eclipse,
        double latitudeDeg,
        double longitudeDeg,
        double altitudeMeters);
}