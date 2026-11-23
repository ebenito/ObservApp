namespace ObservApp.Shared.Services;

using ObservApp.Shared.Models;

/// <summary>
/// Lee y escribe el archivo SQLite de TiempoUbicacionApp (<c>Ubicaciones.db</c>, tabla
/// <c>LocationEntry</c>).
/// </summary>
public interface ILocationDbFileCodec
{
	/// <summary>Lee las filas del archivo. Nunca lanza por un archivo no válido: lo indica en el resultado.</summary>
	Task<LocationDbReadResult> ReadAsync(byte[] fileBytes);

	/// <summary>Genera un archivo SQLite nuevo con las filas indicadas.</summary>
	Task<byte[]> WriteAsync(IReadOnlyList<LegacyLocationEntry> rows);
}

public enum LocationDbReadStatus
{
	Ok,
	/// <summary>El archivo no es una base de datos SQLite.</summary>
	NotSqlite,
	/// <summary>Es SQLite pero no tiene la tabla <c>LocationEntry</c>.</summary>
	MissingTable
}

public sealed record LocationDbReadResult(LocationDbReadStatus Status, IReadOnlyList<LegacyLocationEntry> Rows);
