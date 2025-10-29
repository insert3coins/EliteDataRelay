using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Configuration
{
    /// <summary>
    /// A custom JSON converter for booleans that ensures they are always written to the file.
    /// </summary>
    public class JsonBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetBoolean();
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
