using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HsnSoft.Base.Json.SystemTextJson.JsonConverters;

public class BaseStringToEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    private readonly JsonStringEnumConverter _innerJsonStringEnumConverter;

    private JsonSerializerOptions _readJsonSerializerOptions;

    private JsonSerializerOptions _writeJsonSerializerOptions;

    public BaseStringToEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    public BaseStringToEnumConverter(JsonNamingPolicy namingPolicy = null, bool allowIntegerValues = true)
    {
        _innerJsonStringEnumConverter = new JsonStringEnumConverter(namingPolicy, allowIntegerValues);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        _readJsonSerializerOptions ??= JsonSerializerOptionsHelper.Create(options, x =>
                x == this ||
                x.GetType() == typeof(BaseStringToEnumFactory),
            _innerJsonStringEnumConverter.CreateConverter(typeToConvert, options));

        return JsonSerializer.Deserialize<T>(ref reader, _readJsonSerializerOptions);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        _writeJsonSerializerOptions ??= JsonSerializerOptionsHelper.Create(options, x =>
            x == this ||
            x.GetType() == typeof(BaseStringToEnumFactory));

        JsonSerializer.Serialize(writer, value, _writeJsonSerializerOptions);
    }
}