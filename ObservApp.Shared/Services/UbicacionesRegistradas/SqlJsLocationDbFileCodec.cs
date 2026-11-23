namespace ObservApp.Shared.Services;

using System.Text;
using Microsoft.JSInterop;
using ObservApp.Shared.Models;

/// <summary>
/// <see cref="ILocationDbFileCodec"/> con sql.js (<c>wwwroot/location-db.js</c>). Sirve para
/// MAUI y Web porque en ambos la UI corre en un navegador.
/// </summary>
public class SqlJsLocationDbFileCodec : ILocationDbFileCodec
{
	private static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

	private readonly IJSRuntime _js;

	public SqlJsLocationDbFileCodec(IJSRuntime js)
	{
		_js = js;
	}

	/// <summary>Comprueba la cabecera de 16 bytes de un archivo SQLite 3.</summary>
	public static bool HasSqliteHeader(ReadOnlySpan<byte> bytes) =>
		bytes.Length >= SqliteHeader.Length && bytes[..SqliteHeader.Length].SequenceEqual(SqliteHeader);

	public async Task<LocationDbReadResult> ReadAsync(byte[] fileBytes)
	{
		if (!HasSqliteHeader(fileBytes))
		{
			return new LocationDbReadResult(LocationDbReadStatus.NotSqlite, Array.Empty<LegacyLocationEntry>());
		}

		var result = await _js.InvokeAsync<JsReadResult>("observApp.locationDb.read", fileBytes);

		return result.Error switch
		{
			null => new LocationDbReadResult(LocationDbReadStatus.Ok, result.Rows ?? new List<LegacyLocationEntry>()),
			"no_table" => new LocationDbReadResult(LocationDbReadStatus.MissingTable, Array.Empty<LegacyLocationEntry>()),
			_ => new LocationDbReadResult(LocationDbReadStatus.NotSqlite, Array.Empty<LegacyLocationEntry>())
		};
	}

	public async Task<byte[]> WriteAsync(IReadOnlyList<LegacyLocationEntry> rows) =>
		await _js.InvokeAsync<byte[]>("observApp.locationDb.write", rows);

	private sealed class JsReadResult
	{
		public string? Error { get; set; }
		public List<LegacyLocationEntry>? Rows { get; set; }
	}
}
