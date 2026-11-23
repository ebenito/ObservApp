namespace ObservApp.Shared.Services;

/// <summary>
/// Guarda un archivo generado en memoria donde el usuario elija.
/// MAUI: selector nativo de "Guardar como" (CommunityToolkit FileSaver).
/// Web: descarga del navegador.
/// </summary>
public interface IFileSaveService
{
	/// <summary>
	/// Devuelve true si el archivo se guardó (o la descarga se lanzó), false si el usuario
	/// canceló o falló (ver <see cref="LastError"/>).
	/// </summary>
	Task<bool> SaveAsync(string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default);

	/// <summary>Último error, ya traducido. Nulo si la última operación fue bien o se canceló.</summary>
	string? LastError { get; }
}
