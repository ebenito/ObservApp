namespace ObservApp.Shared.Services;

public record EclipseDefinition(
    string Id,
    DateTime Date,          // fecha UTC aproximada del máximo
    EclipseType MaxType,    // tipo máximo global
    string RegionLabel      // "América del Norte", "Europa", etc.
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

public record LocalEclipseResult(
    LocalEclipseType Type,
    double MaxCoverage,         // 0..1
    List<EclipseContact> Contacts,
    bool IsVisible              // false si el Sol está bajo el horizonte
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