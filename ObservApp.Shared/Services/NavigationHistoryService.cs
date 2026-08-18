namespace ObservApp.Shared.Services;

/// <summary>
/// Implementación del servicio de historial de navegación.
/// Mantiene un stack de URLs visitadas para permitir volver a la página anterior.
/// </summary>
public class NavigationHistoryService : INavigationHistoryService
{
	private readonly Stack<string> _history = [];

	/// <summary>
	/// Registra la URL actual en el historial de navegación.
	/// </summary>
	/// <param name="url">URL a registrar</param>
	public void PushUrl(string url)
	{
		if (!string.IsNullOrWhiteSpace(url))
		{
			_history.Push(url);
		}
	}

	/// <summary>
	/// Obtiene la URL anterior en el historial y elimina la entrada actual.
	/// Si el historial es vacío o solo contiene una entrada, devuelve la URL por defecto.
	/// </summary>
	/// <param name="defaultUrl">URL por defecto si no hay historial disponible</param>
	/// <returns>La URL anterior o la URL por defecto</returns>
	public string PopUrl(string defaultUrl = "/")
	{
		// Eliminar la URL actual (la primera en el stack)
		if (_history.Count > 0)
		{
			_history.Pop();
		}

		// Retornar la URL anterior o la por defecto
		return _history.Count > 0 ? _history.Pop() : defaultUrl;
	}

	/// <summary>
	/// Limpia el historial de navegación.
	/// </summary>
	public void Clear()
	{
		_history.Clear();
	}

	/// <summary>
	/// Obtiene el número de entradas en el historial.
	/// </summary>
	public int Count => _history.Count;
}
