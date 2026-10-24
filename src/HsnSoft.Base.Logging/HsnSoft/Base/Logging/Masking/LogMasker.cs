using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HsnSoft.Base.Logging.Masking;

public sealed class LogMasker : ILogMasker
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // people read these logs: Turkish letters and the like stay as they are, not \u015F
    private static readonly JsonSerializerOptions s_writeOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly IReadOnlyDictionary<string, SensitiveDataAttribute> s_noKeys = new Dictionary<string, SensitiveDataAttribute>();

    private readonly Func<object, object> _maskObject;
    private readonly SensitiveDataRegistry? _registry;
    private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, SensitiveDataAttribute>> _withShape = new();

    public LogMasker()
        : this((SensitiveDataRegistry?)null)
    {
    }

    /// <param name="registry">The sensitive properties of the contract classes the application registered; they are hidden in every JSON text.</param>
    public LogMasker(SensitiveDataRegistry? registry)
        : this(value => JsonDataMasking.MaskSensitiveData(value), registry)
    {
    }

    internal LogMasker(Func<object, object> maskObject, SensitiveDataRegistry? registry = null)
    {
        _maskObject = maskObject;
        _registry = registry;
    }

    public string MaskText(string? text) => MaskText(text, null);

    public string MaskText(string? text, Type? shape)
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

            MaskNode(node, KeysFor(shape));
            return node.ToJsonString(s_writeOptions);
        }
        catch (JsonException)
        {
            // not JSON: nothing to look into
            return text;
        }
    }

    // the keys of the registered contracts, and of the class the text is the JSON of, whose attributes say how each value is hidden
    private IReadOnlyDictionary<string, SensitiveDataAttribute> KeysFor(Type? shape)
    {
        if (shape is null)
        {
            return _registry?.Keys ?? s_noKeys;
        }

        return _withShape.GetOrAdd(shape, type =>
        {
            Dictionary<string, SensitiveDataAttribute> keys = new(_registry?.Keys ?? s_noKeys, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, SensitiveDataAttribute> pair in SensitiveDataShape.Of(type))
            {
                keys[pair.Key] = pair.Value;
            }

            return keys;
        });
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
            string masked = MaskText(JsonSerializer.Serialize(value, s_jsonOptions), typeof(T));
            return JsonSerializer.Deserialize<object>(masked, s_jsonOptions)!;
        }
        catch (Exception)
        {
            return $"[{typeof(T).Name} could not be masked and is not logged]";
        }
    }

    private static void MaskNode(JsonNode node, IReadOnlyDictionary<string, SensitiveDataAttribute> keys)
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

                    // a property the class marked hides its whole value, as the attribute says; the usual key names hide a text
                    if (keys.TryGetValue(property.Key, out SensitiveDataAttribute? attribute))
                    {
                        obj[property.Key] = MaskValue(property.Value, attribute);
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

                    MaskNode(property.Value, keys);
                }

                break;

            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    if (item is not null)
                    {
                        MaskNode(item, keys);
                    }
                }

                break;
        }
    }

    // a marked property that holds a list or an object hides every value in it
    private static JsonNode? MaskValue(JsonNode? node, SensitiveDataAttribute attribute)
    {
        return node switch
        {
            JsonValue scalar => (JsonNode?)MaskingHelper.MaskByAttribute(ScalarText(scalar), attribute),
            JsonArray array => new JsonArray([.. array.Select(item => MaskValue(item, attribute))]),
            JsonObject obj => new JsonObject(obj.Select(pair => KeyValuePair.Create(pair.Key, MaskValue(pair.Value, attribute)))),
            _ => null,
        };
    }

    // only used to see whether masking changes the value, so the text of a number or a boolean is as good as the value itself
    private static string? ScalarText(JsonValue value)
        => value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
}
