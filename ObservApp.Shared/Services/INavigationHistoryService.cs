namespace ObservApp.Shared.Services;

/// <summary>
/// Servicio para gestionar el historial de navegación y permitir volver a la página anterior.
/// </summary>
public interface INavigationHistoryService
{
	/// <summary>
	/// Registra la URL actual en el historial de navegación.
	/// </summary>
	/// <param name="url">URL a registrar</param>
	void PushUrl(string url);

	/// <summary>
	/// Obtiene la URL anterior en el historial y elimina la entrada actual.
	/// Si el historial es vacío o solo contiene una entrada, devuelve la URL por defecto.
	/// </summary>
	/// <param name="defaultUrl">URL por defecto si no hay historial disponible</param>
	/// <returns>La URL anterior o la URL por defecto</returns>
	string PopUrl(string defaultUrl = "/");

	/// <summary>
	/// Limpia el historial de navegación.
	/// </summary>
	void Clear();

	/// <summary>
	/// Obtiene el número de entradas en el historial.
	/// </summary>
	/// <returns>Número de URLs en el historial</returns>
	int Count { get; }
}
