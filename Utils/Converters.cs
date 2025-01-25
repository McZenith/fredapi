using System.Text.Json;
using System.Text.Json.Serialization;

namespace fredapi.Utils.Converters;

public class FlexibleNumberConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                    return intValue;
                if (reader.TryGetDouble(out double doubleValue))
                    return doubleValue;
                throw new JsonException($"Unable to convert {reader.TokenType} to number");

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue)) return null;
                if (int.TryParse(stringValue, out int parsedInt))
                    return parsedInt;
                if (double.TryParse(stringValue, out double parsedDouble))
                    return parsedDouble;
                return stringValue;

            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            default:
                throw new JsonException($"Unexpected value type {value.GetType()}");
        }
    }
}

public class FlexibleStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long longValue))
                    return longValue.ToString();
                if (reader.TryGetDouble(out double doubleValue))
                    return doubleValue.ToString();
                throw new JsonException($"Unable to convert {reader.TokenType} to string");

            case JsonTokenType.True:
                return "true";

            case JsonTokenType.False:
                return "false";

            case JsonTokenType.Null:
                return null;

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStringValue(value);
    }
}

public class FlexibleBooleanConverter : JsonConverter<bool>
{
    private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "1", "yes", "y", "on", "enabled"
    };

    private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "false", "0", "no", "n", "off", "disabled"
    };

    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return false;

                if (TrueValues.Contains(stringValue))
                    return true;
                
                if (FalseValues.Contains(stringValue))
                    return false;

                throw new JsonException($"Cannot convert string value '{stringValue}' to boolean");

            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long longValue))
                    return longValue != 0;

                if (reader.TryGetDouble(out double doubleValue))
                    return !double.IsNaN(doubleValue) && Math.Abs(doubleValue) > double.Epsilon;

                throw new JsonException($"Unable to convert number to boolean");

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

public class NullableFlexibleBooleanConverter : JsonConverter<bool?>
{
    private readonly FlexibleBooleanConverter _booleanConverter = new();

    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        try
        {
            return _booleanConverter.Read(ref reader, typeof(bool), options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteBooleanValue(value.Value);
    }
}