using System.Text.Json;
using ObservApp.Shared.Models;
using Xunit;

namespace ObservApp.Shared.Tests.Services.UbicacionesRegistradas;

/// <summary>
/// Reproduce el camino de Supabase.Postgrest 8.14 al insertar: serializa el modelo, lo pasa a
/// Dictionary&lt;string, object&gt; con sus convertidores y lo vuelve a serializar.
/// </summary>
public sealed class LocationEntrySerializationTests
{
	private const long Ticks = 638_899_171_523_191_677; // primer registro real de Ubicaciones.db

	private static readonly JsonSerializerOptions PostgrestOptions =
		Supabase.Postgrest.Client.SerializerSettings(new Supabase.Postgrest.ClientOptions());

	[Fact]
	public void InsertPayload_KeepsDateTicksExact()
	{
		var entry = new LocationEntry { DateTicks = Ticks };

		var json = JsonSerializer.Serialize(entry, PostgrestOptions);
		var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, PostgrestOptions);
		var payload = JsonSerializer.Serialize(dict, PostgrestOptions);

		Assert.Contains("\"date_ticks\":\"638899171523191677\"", payload);
	}

	[Fact]
	public void Read_IgnoresGeneratedDateLocalColumn()
	{
		// date_local es una columna generada (migración 20261123160000) que el modelo no mapea.
		var json = "{\"date_ticks\":638899171523191677,\"date_local\":\"2025-08-04T15:12:32.319167\",\"location\":\"X\"}";

		var entry = JsonSerializer.Deserialize<LocationEntry>(json, PostgrestOptions);

		Assert.Equal(Ticks, entry!.DateTicks);
		Assert.Equal("X", entry.Location);
	}

	[Fact]
	public void InsertPayload_DoesNotSendDateLocal()
	{
		var json = JsonSerializer.Serialize(new LocationEntry { DateTicks = Ticks }, PostgrestOptions);

		Assert.DoesNotContain("date_local", json);
	}

	[Fact]
	public void InsertPayload_IncludesFavoriteColumns()
	{
		var favoriteId = Guid.Parse("5b1f0c2e-8f0a-4a57-9a43-2f1d5c3b7e10");
		var entry = new LocationEntry
		{
			IsFavorite = true,
			Source = LocationEntrySources.Favorite,
			FavoriteId = favoriteId,
			AltitudeMeters = 2150
		};

		var json = JsonSerializer.Serialize(entry, PostgrestOptions);

		Assert.Contains("\"is_favorite\":true", json);
		Assert.Contains("\"source\":\"favorite\"", json);
		Assert.Contains($"\"favorite_id\":\"{favoriteId}\"", json);
		Assert.Contains("\"altitude_m\":2150", json);
	}

	[Fact]
	public void Read_RowWithoutNewColumns_IsUnflaggedImport()
	{
		// Fila anterior a la migración 20261124120000 (o leída antes de aplicarla).
		var entry = JsonSerializer.Deserialize<LocationEntry>("{\"location\":\"X\"}", PostgrestOptions);

		Assert.False(entry!.IsFavorite);
		Assert.Equal(LocationEntrySources.Import, entry.Source);
		Assert.Null(entry.FavoriteId);
		Assert.Null(entry.AltitudeMeters);
	}

	[Theory]
	[InlineData("{\"date_ticks\":638899171523191677}")]   // lo que devuelve PostgREST (bigint)
	[InlineData("{\"date_ticks\":\"638899171523191677\"}")]
	public void Read_AcceptsNumberOrString(string json)
	{
		var entry = JsonSerializer.Deserialize<LocationEntry>(json, PostgrestOptions);

		Assert.Equal(Ticks, entry!.DateTicks);
	}
}
