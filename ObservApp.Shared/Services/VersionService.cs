using System.Reflection;

namespace ObservApp.Shared.Services;

/// <summary>
/// Servicio centralizado para obtener información de versión de la aplicación.
/// Funciona en todas las plataformas: Windows, Android y WebAssembly.
/// </summary>
public interface IVersionService
{
	/// <summary>
	/// Obtiene la versión en formato "mayor.menor.patch" (ej: "1.3.2")
	/// </summary>
	string GetFullVersion();

	/// <summary>
	/// Obtiene la versión formateada para UI (ej: "v1.3 · .NET 10")
	/// </summary>
	string GetFormattedVersion();

	/// <summary>
	/// Obtiene solo la versión major.minor (ej: "1.3")
	/// </summary>
	string GetMajorMinorVersion();

	/// <summary>
	/// Obtiene la versión del framework objetivo
	/// </summary>
	string GetTargetFramework();
}

/// <summary>
/// Implementación del servicio de versión.
/// </summary>
public class VersionService : IVersionService
{
	private readonly Version? _assemblyVersion;
	private const string DefaultVersion = "1.0.0";
	private const string DefaultFramework = ".NET 10";

	public VersionService()
	{
		// Intenta obtener la versión del assembly
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			_assemblyVersion = assembly.GetName().Version;
		}
		catch
		{
			_assemblyVersion = null;
		}
	}

	/// <summary>
	/// Obtiene la versión completa con fallback si no está disponible
	/// </summary>
	public string GetFullVersion()
	{
		if (_assemblyVersion != null)
		{
			return _assemblyVersion.ToString(3); // major.minor.patch
		}

		return DefaultVersion;
	}

	/// <summary>
	/// Obtiene la versión formateada para mostrar en UI
	/// </summary>
	public string GetFormattedVersion()
	{
		var version = GetMajorMinorVersion();
		var framework = GetTargetFramework();
		return $"v{version} · {framework}";
	}

	/// <summary>
	/// Obtiene versión major.minor (sin patch)
	/// </summary>
	public string GetMajorMinorVersion()
	{
		if (_assemblyVersion != null)
		{
			return $"{_assemblyVersion.Major}.{_assemblyVersion.Minor}";
		}

		return "1.0";
	}

	/// <summary>
	/// Obtiene el nombre del framework objetivo
	/// </summary>
	public string GetTargetFramework()
	{
		return DefaultFramework;
	}
}
