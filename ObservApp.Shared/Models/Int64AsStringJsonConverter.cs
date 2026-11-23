namespace ObservApp.Shared.Models;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Escribe un <see cref="long"/> como cadena JSON y lo lee tanto de cadena como de número.
/// </summary>
/// <remarks>
/// Supabase.Postgrest 8.14 serializa el modelo, lo convierte a <c>Dictionary&lt;string, object&gt;</c>
/// con <c>ObjectToInferredTypesConverter</c> y lo vuelve a serializar; en ese paso los enteros
/// grandes pasan a <see cref="double"/> (<c>638899171523191677</c> → <c>6.388991715231917E+17</c>)
/// y PostgreSQL rechaza el valor en una columna <c>bigint</c> (error 22P02). Como cadena el
/// valor llega intacto y PostgreSQL lo convierte a <c>bigint</c>.
/// </remarks>
public sealed class Int64AsStringJsonConverter : JsonConverter<long>
{
	public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.TokenType switch
		{
			JsonTokenType.Number => reader.GetInt64(),
			JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
			JsonTokenType.Null => 0,
			_ => throw new JsonException($"No se puede convertir {reader.TokenType} a Int64.")
		};

	public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
