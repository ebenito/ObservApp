using System.Globalization;

namespace ObservApp.Shared.Models;

/// <summary>
/// Configuración de exposición fotográfica enviable a una cámara física a
/// través de <see cref="ObservApp.Shared.Interfaces.ICameraService"/>.
/// Los valores se expresan en el mismo formato fotográfico que ya usa el
/// resto de la app (ver <c>CalculadoraEclipse.razor</c>, sección de tabla de
/// velocidades): Shutter "1/250" o "2" (segundos), Iso 100, Aperture "f/8".
/// </summary>
/// <param name="Shutter">Velocidad de obturación, p. ej. "1/250", "2", "0.5".</param>
/// <param name="Iso">Sensibilidad ISO, p. ej. 100.</param>
/// <param name="Aperture">Apertura del diafragma, p. ej. "f/8".</param>
public sealed record ExposureSetting(string Shutter, int Iso, string Aperture)
{
    /// <summary>
    /// Convierte <see cref="Shutter"/> a segundos. Necesario para backends
    /// que esperan valores numéricos: PTP/IP (décimas de segundo), el cálculo
    /// del bracket de corona en <c>CameraManager</c>, etc.
    /// Devuelve 0 si el valor no se puede interpretar.
    /// </summary>
    public double ShutterSeconds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Shutter)) return 0;

            // Admite la notación con comillas para segundos enteros (ej. 2")
            // usada habitualmente en fotografía ("2 segundos").
            var s = Shutter.Trim().TrimEnd('"');

            if (s.Contains('/'))
            {
                var parts = s.Split('/', 2);
                return double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                       double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var den) &&
                       den != 0
                    ? num / den
                    : 0;
            }

            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds)
                ? seconds
                : 0;
        }
    }

    public override string ToString() => $"{Aperture} · ISO {Iso} · {Shutter}";
}
