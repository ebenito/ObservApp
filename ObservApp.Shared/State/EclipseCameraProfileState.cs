namespace ObservApp.Shared.State;

/// <summary>
/// Estado reactivo singleton que comparte la configuración de equipo
/// (apertura del diafragma e ISO) entre la Calculadora de Exposición de
/// Eclipses — donde se introduce/calcula — y el Timer de Eclipse — donde se
/// consume para generar la tabla de exposiciones por evento (ver
/// <see cref="Shared.Services.EclipseExposureCalculator"/>).
///
/// Sigue el mismo patrón que <see cref="AppState"/>: Singleton + evento de
/// cambio, sin persistencia en disco — se recalcula cada vez que el usuario
/// visita la Calculadora de Exposición, igual que el resto de inputs de las
/// calculadoras de la app.
/// </summary>
public sealed class EclipseCameraProfileState
{
	private double _apertureFNumber;
	private int _iso = 100;

	/// <summary>Se dispara cuando cambia la apertura o el ISO configurados.</summary>
	public event Action? OnChanged;

	/// <summary>Número f del diafragma configurado, p. ej. 8 para f/8. 0 = todavía sin configurar.</summary>
	public double ApertureFNumber
	{
		get => _apertureFNumber;
		set
		{
			if (_apertureFNumber == value) return;
			_apertureFNumber = value;
			OnChanged?.Invoke();
		}
	}

	/// <summary>Sensibilidad ISO configurada, p. ej. 100.</summary>
	public int Iso
	{
		get => _iso;
		set
		{
			if (_iso == value) return;
			_iso = value;
			OnChanged?.Invoke();
		}
	}

	/// <summary>True si ya se ha calculado al menos una vez en la Calculadora de Exposición.</summary>
	public bool IsConfigured => _apertureFNumber > 0;

	/// <summary>Actualiza apertura e ISO a la vez, disparando una única notificación.</summary>
	public void SetEquipment(double apertureFNumber, int iso)
	{
		if (_apertureFNumber == apertureFNumber && _iso == iso) return;
		_apertureFNumber = apertureFNumber;
		_iso = iso;
		OnChanged?.Invoke();
	}
}