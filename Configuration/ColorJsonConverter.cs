using System;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteDataRelay.Configuration
{
    /// <summary>
    /// A custom JSON converter for System.Drawing.Color.
    /// Serializes a Color to a hex string (e.g., "#AARRGGBB") and deserializes it back.
    /// </summary>
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected a string for Color value.");
            }

            string? colorString = reader.GetString();
            if (string.IsNullOrEmpty(colorString) || !colorString.StartsWith("#") || colorString.Length != 9)
            {
                return Color.Empty; // Or a default color
            }

            int argb = int.Parse(colorString.Substring(1), NumberStyles.HexNumber);
            return Color.FromArgb(argb);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            // Serialize the color as #AARRGGBB
            writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
        }
    }
}