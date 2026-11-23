namespace ObservApp.Shared.Models;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

/// <summary>
/// Registro de ubicación compatible con la tabla <c>LocationEntry</c> de TiempoUbicacionApp
/// (SQLite, base de datos <c>Ubicaciones.db</c>). Se persiste en Supabase en
/// <c>location_entries</c>.
/// </summary>
/// <remarks>
/// Los campos de texto se guardan tal cual llegan de TiempoUbicacionApp (coordenadas en DMS,
/// horas y desfase formateados) para que exportar devuelva el mismo contenido.
/// <see cref="LatitudeDeg"/> y <see cref="LongitudeDeg"/> son la conversión a grados decimales
/// que usa el mapa; son nulas cuando el texto no se pudo interpretar.
/// </remarks>
[Table("location_entries")]
public class LocationEntry : BaseModel
{
	[PrimaryKey("id", false)]
	public Guid Id { get; set; } = Guid.NewGuid();

	[Column("user_id")]
	public string UserId { get; set; } = string.Empty;

	/// <summary>
	/// Columna <c>Date</c> de SQLite tal cual: <see cref="DateTime.Ticks"/> de la hora local del
	/// dispositivo, sin zona horaria. Se guarda como entero para que la ida y vuelta sea exacta.
	/// </summary>
	[Column("date_ticks")]
	[System.Text.Json.Serialization.JsonConverter(typeof(Int64AsStringJsonConverter))]
	public long DateTicks { get; set; }


	[Column("fecha_actual")]
	public string FechaActual { get; set; } = string.Empty;

	[Column("hora_local")]
	public string HoraLocal { get; set; } = string.Empty;

	[Column("hora_utc")]
	public string HoraUtc { get; set; } = string.Empty;

	[Column("desfase")]
	public string Desfase { get; set; } = string.Empty;

	[Column("tipo_horario")]
	public string TipoHorario { get; set; } = string.Empty;

	/// <summary>Nombre del lugar (columna <c>Location</c>).</summary>
	[Column("location")]
	public string Location { get; set; } = string.Empty;

	/// <summary>Latitud original en texto, normalmente DMS (<c>40°51'33" N</c>).</summary>
	[Column("latitude")]
	public string Latitude { get; set; } = string.Empty;

	/// <summary>Longitud original en texto, normalmente DMS (<c>2°11'48" O</c>).</summary>
	[Column("longitude")]
	public string Longitude { get; set; } = string.Empty;

	[Column("zona")]
	public int Zona { get; set; }

	[Column("latitude_deg")]
	public double? LatitudeDeg { get; set; }

	[Column("longitude_deg")]
	public double? LongitudeDeg { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Métodos y no propiedades: así ningún serializador de Supabase (Newtonsoft hasta 8.9,
	// System.Text.Json desde 8.14) intenta mandarlos como columnas.

	/// <summary>Fecha local del registro (sin zona horaria); <see cref="DateTime.MinValue"/> si no hay.</summary>
	public DateTime GetDate() =>
		DateTicks > 0 && DateTicks <= DateTime.MaxValue.Ticks
			? new DateTime(DateTicks, DateTimeKind.Unspecified)
			: DateTime.MinValue;

	/// <summary>True si el registro tiene coordenadas utilizables en el mapa.</summary>
	public bool HasCoordinates() => LatitudeDeg.HasValue && LongitudeDeg.HasValue;
}

/// <summary>
/// Fila de la tabla <c>LocationEntry</c> de TiempoUbicacionApp tal como la lee o escribe
/// el códec SQLite (sin atributos de Supabase).
/// </summary>
public sealed class LegacyLocationEntry
{
	public long Id { get; set; }

	/// <summary>
	/// Valor de la columna <c>Date</c> en texto. sqlite-net guarda <see cref="DateTime.Ticks"/>
	/// como entero; se transporta como cadena porque los ticks superan la precisión de un
	/// número de JavaScript.
	/// </summary>
	public string? DateValue { get; set; }

	/// <summary>Tipo SQLite de <c>Date</c> (<c>integer</c>, <c>text</c>, <c>real</c> o <c>null</c>).</summary>
	public string? DateType { get; set; }

	public string? FechaActual { get; set; }
	public string? HoraLocal { get; set; }
	public string? HoraUtc { get; set; }
	public string? Desfase { get; set; }
	public string? TipoHorario { get; set; }
	public string? Location { get; set; }
	public string? Latitude { get; set; }
	public string? Longitude { get; set; }
	public long Zona { get; set; }
}
