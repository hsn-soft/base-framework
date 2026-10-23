using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HsnSoft.Base.Logging.Masking;

public sealed class LogMasker : ILogMasker
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Func<object, object> _maskObject;

    public LogMasker()
        : this(value => JsonDataMasking.MaskSensitiveData(value))
    {
    }

    internal LogMasker(Func<object, object> maskObject) => _maskObject = maskObject;

    public string MaskText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        try
        {
            JsonNode? node = JsonNode.Parse(text);
            if (node is null)
            {
                return text;
            }

            MaskNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            // not JSON: nothing to look into
            return text;
        }
    }

    public object MaskObject<T>(T value)
    {
        if (value is null)
        {
            return new { };
        }

        try
        {
            return _maskObject(value);
        }
        catch (Exception)
        {
            return MaskThroughJson(value);
        }
    }

    // the second way: look at the object as JSON and hide what its key names give away; when even that fails nothing is returned
    // that could contain what the first way could not hide
    private object MaskThroughJson<T>(T value)
    {
        try
        {
            string masked = MaskText(JsonSerializer.Serialize(value, s_jsonOptions));
            return JsonSerializer.Deserialize<object>(masked, s_jsonOptions)!;
        }
        catch (Exception)
        {
            return $"[{typeof(T).Name} could not be masked and is not logged]";
        }
    }

    private static void MaskNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (KeyValuePair<string, JsonNode?> property in obj.ToList())
                {
                    if (property.Value is null)
                    {
                        continue;
                    }

                    if (property.Value is JsonValue scalar)
                    {
                        string? raw = ScalarText(scalar);
                        string? masked = MaskingHelper.MaskByKeyName(raw, property.Key);
                        if (raw != masked)
                        {
                            obj[property.Key] = masked;
                        }

                        continue;
                    }

                    MaskNode(property.Value);
                }

                break;

            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    if (item is not null)
                    {
                        MaskNode(item);
                    }
                }

                break;
        }
    }

    // only used to see whether masking changes the value, so the text of a number or a boolean is as good as the value itself
    private static string? ScalarText(JsonValue value)
        => value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
}
