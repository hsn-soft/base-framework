using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog.Mask;

namespace HsnSoft.Base.Serilog;

public sealed class LogMasker : ILogMasker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string MaskText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        try
        {
            var node = JsonNode.Parse(text);
            if (node == null)
                return text;

            MaskNode(node, null);
            return node.ToJsonString();
        }
        catch
        {
            return text;
        }
    }

    public object MaskObject<T>(T value)
    {
        if (value == null!)
            return new { };

        try
        {
            return JsonDataMasking.MaskSensitiveData(value!);
        }
        catch
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                var masked = MaskText(json);
                return JsonSerializer.Deserialize<object>(masked, JsonOptions) ?? new { };
            }
            catch
            {
                return value!;
            }
        }
    }

    private static void MaskNode(JsonNode node, string? parentKey)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj.ToList())
                {
                    if (kv.Key != null && kv.Value != null)
                    {
                        if (kv.Value is JsonValue jsonValue)
                        {
                            string? rawValue = TryGetScalarValueAsString(jsonValue);
                            string? masked = MaskingHelper.MaskByKeyName(rawValue, kv.Key);

                            if (rawValue != masked)
                            {
                                obj[kv.Key] = masked;
                                continue;
                            }
                        }

                        MaskNode(kv.Value, kv.Key);
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    if (item != null)
                        MaskNode(item, parentKey);
                }
                break;
        }
    }

    private static string? TryGetScalarValueAsString(JsonValue value)
    {
        try
        {
            if (value.TryGetValue<string>(out var s))
                return s;

            if (value.TryGetValue<int>(out var i))
                return i.ToString();

            if (value.TryGetValue<long>(out var l))
                return l.ToString();

            if (value.TryGetValue<decimal>(out var d))
                return d.ToString();

            if (value.TryGetValue<double>(out var dbl))
                return dbl.ToString();

            if (value.TryGetValue<bool>(out var b))
                return b.ToString();

            return value.ToJsonString();
        }
        catch
        {
            return value.ToJsonString();
        }
    }
}