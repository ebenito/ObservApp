namespace ObservApp.Shared.Models;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("favorite_locations")]
public class FavoriteLocation : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("altitude_m")]
    public double AltitudeMeters { get; set; }

    [Column("is_favorite")]
    public bool IsFavorite { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Convierte a DTO para persistencia local (sin atributos de Supabase).
    /// </summary>
    public FavoriteLocationDto ToDto() => new()
    {
        Id = this.Id,
        UserId = this.UserId,
        Name = this.Name,
        Latitude = this.Latitude,
        Longitude = this.Longitude,
        AltitudeMeters = this.AltitudeMeters,
        IsFavorite = this.IsFavorite,
        CreatedAt = this.CreatedAt
    };

    /// <summary>
    /// Convierte desde DTO a FavoriteLocation.
    /// </summary>
    public static FavoriteLocation FromDto(FavoriteLocationDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        Name = dto.Name,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        AltitudeMeters = dto.AltitudeMeters,
        IsFavorite = dto.IsFavorite,
        CreatedAt = dto.CreatedAt
    };
}

/// <summary>
/// DTO simple para serialización JSON local (sin atributos de Supabase).
/// Se usa para almacenamiento en Preferences/localStorage.
/// </summary>
public class FavoriteLocationDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeMeters { get; set; }
    public bool IsFavorite { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
