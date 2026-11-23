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

	[Theory]
	[InlineData("{\"date_ticks\":638899171523191677}")]   // lo que devuelve PostgREST (bigint)
	[InlineData("{\"date_ticks\":\"638899171523191677\"}")]
	public void Read_AcceptsNumberOrString(string json)
	{
		var entry = JsonSerializer.Deserialize<LocationEntry>(json, PostgrestOptions);

		Assert.Equal(Ticks, entry!.DateTicks);
	}
}
