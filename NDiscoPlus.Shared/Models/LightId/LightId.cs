using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NDiscoPlus.Shared.Models;
public abstract class LightId
{
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public static bool operator ==(LightId a, object? b)
        => a.Equals(b);
    public static bool operator !=(LightId a, object? b)
        => !a.Equals(b);
}

internal class JsonLightIdConverter : JsonConverter<LightId>
{
    static (string type, int value) Serialize(LightId id)
    {
        string type;
        int value;

        if (id is HueLightId hli)
        {
            type = "hue";
            value = hli.ChannelId;
        }
        else if (id is ScreenLightId sli)
        {
            type = "screen";
            value = sli.Index;
        }
        else
        {
            throw new NotImplementedException();
        }

        return (type, value);
    }

    static LightId Deserialize(string type, int value)
    {
        return type switch
        {
            "hue" => new HueLightId((byte)value),
            "screen" => new ScreenLightId(value),
            _ => throw new NotImplementedException()
        };
    }

    public override LightId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        string? type = null;
        int? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (type is null || !value.HasValue)
                    throw new JsonException();
                return Deserialize(type, value.Value);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string? propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "type":
                    type = reader.GetString();
                    break;
                case "value":
                    value = reader.GetInt32();
                    break;
                default:
                    throw new JsonException();
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, LightId value, JsonSerializerOptions options)
    {
        (string type, int val) = Serialize(value);

        writer.WriteStartObject();
        writer.WriteString("type", type);
        writer.WriteNumber("value", val);
        writer.WriteEndObject();
    }
}