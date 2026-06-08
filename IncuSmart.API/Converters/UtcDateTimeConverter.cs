using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncuSmart.API.Converters
{
    // Kind=Local → ToUniversalTime() gives true UTC (handles Npgsql legacy mode returning timestamptz as local)
    // Kind=Utc   → no-op
    // Kind=Unspecified → assume UTC (our code always stores UTC values)
    internal static class DateTimeNormalizer
    {
        internal static DateTime ToUtc(DateTime dt) => dt.Kind == DateTimeKind.Local
            ? dt.ToUniversalTime()
            : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(DateTimeNormalizer.ToUtc(value));
    }

    public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }
            writer.WriteStringValue(DateTimeNormalizer.ToUtc(value.Value));
        }
    }
}
