using System;
using Newtonsoft.Json;

namespace Ssit.CrossX2.Framework;

/// <summary>
/// RgbaColor only exposes its channels as readonly fields set via a primary constructor,
/// so Newtonsoft's default contract resolver can read them but has no writable member to
/// assign on deserialize - it silently falls back to a zeroed struct. This converter
/// round-trips the channels explicitly.
/// </summary>
public class RgbaColorJsonConverter : JsonConverter<RgbaColor>
{
    public override void WriteJson(JsonWriter writer, RgbaColor value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("R");
        writer.WriteValue(value.R);
        writer.WritePropertyName("G");
        writer.WriteValue(value.G);
        writer.WritePropertyName("B");
        writer.WriteValue(value.B);
        writer.WritePropertyName("A");
        writer.WriteValue(value.A);
        writer.WriteEndObject();
    }

    public override RgbaColor ReadJson(JsonReader reader, Type objectType, RgbaColor existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return default;
        }

        var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);

        var r = (byte)(jObject["R"]?.ToObject<int>() ?? 0);
        var g = (byte)(jObject["G"]?.ToObject<int>() ?? 0);
        var b = (byte)(jObject["B"]?.ToObject<int>() ?? 0);
        var a = (byte)(jObject["A"]?.ToObject<int>() ?? 255);

        return new RgbaColor(r, g, b, a);
    }
}
