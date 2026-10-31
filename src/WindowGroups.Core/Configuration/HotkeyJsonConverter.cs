using System.Text.Json;
using System.Text.Json.Serialization;
using WindowGroups.Core.Models;

namespace WindowGroups.Core.Configuration;

/// <summary>
/// Serialises a <see cref="Hotkey"/> as a friendly string such as "Ctrl+Alt+B".
/// Reading tolerates a JSON object form ({"Modifiers": ..., "Key": ...}) for
/// hand-edited files.
/// </summary>
public sealed class HotkeyJsonConverter : JsonConverter<Hotkey>
{
    public override Hotkey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => Hotkey.Parse(reader.GetString()),
            JsonTokenType.StartObject => ReadObject(ref reader),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, Hotkey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private static Hotkey? ReadObject(ref Utf8JsonReader reader)
    {
        var modifiers = HotkeyModifiers.None;
        uint key = 0;
        var hasKey = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var property = reader.GetString();
            reader.Read();

            if (string.Equals(property, "Modifiers", StringComparison.OrdinalIgnoreCase))
            {
                _ = Enum.TryParse<HotkeyModifiers>(reader.GetString(), out modifiers);
            }
            else if (string.Equals(property, "Key", StringComparison.OrdinalIgnoreCase) && reader.TryGetUInt32(out var parsedKey))
            {
                key = parsedKey;
                hasKey = true;
            }
        }

        return hasKey ? new Hotkey(modifiers, key) : null;
    }
}