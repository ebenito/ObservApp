namespace ObservApp.Shared.Models;

using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

/// <summary>
/// Modelo que representa una sesión de observación astronómica.
/// Se persiste en Supabase en la tabla observation_sessions.
/// Extiende BaseModel de Supabase para la serialización/deserialización.
/// </summary>
[Table("observation_sessions")]
public class ObservationSession : BaseModel
{
	/// <summary>Identificador único (UUID).</summary>
	[PrimaryKey("id", false)]
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>Identificador del usuario propietario de la sesión.</summary>
	[Column("user_id")]
	public string UserId { get; set; } = string.Empty;

	/// <summary>Fecha y hora de la observación.</summary>
	[Column("date")]
	public DateTime Date { get; set; } = DateTime.Now;

	/// <summary>Título de la sesión.</summary>
	[Column("title")]
	public string Title { get; set; } = string.Empty;

	/// <summary>Notas adicionales sobre la observación.</summary>
	[Column("notes")]
	public string Notes { get; set; } = string.Empty;

	/// <summary>Latitud del lugar de observación.</summary>
	[Column("latitude")]
	public double? Latitude { get; set; }

	/// <summary>Longitud del lugar de observación.</summary>
	[Column("longitude")]
	public double? Longitude { get; set; }

	/// <summary>Nombre del lugar de observación.</summary>
	[Column("location_name")]
	public string LocationName { get; set; } = string.Empty;

	/// <summary>Índice de seeing (1-5, siendo 5 excelente).</summary>
	[Column("seeing")]
	public SeeingCondition Seeing { get; set; } = SeeingCondition.Average;

	/// <summary>Índice de transparencia (1-5, siendo 5 excelente).</summary>
	[Column("transparency")]
	public TransparencyCondition Transparency { get; set; } = TransparencyCondition.Average;

	/// <summary>Objetos observados durante la sesión.</summary>
	[Column("targets")]
	public List<ObservationTarget> Targets { get; set; } = new();

	/// <summary>Fecha de creación (timestamp en servidor).</summary>
	[Column("created_at")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>Fecha de actualización.</summary>
	[Column("updated_at")]
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum SeeingCondition
{
	Poor = 1,
	BelowAverage = 2,
	Average = 3,
	Good = 4,
	Excellent = 5
}

public enum TransparencyCondition
{
	Poor = 1,
	BelowAverage = 2,
	Average = 3,
	Good = 4,
	Excellent = 5
}
