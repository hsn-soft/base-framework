using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using HsnSoft.Base.Json.Mask.MaskingInfo;

namespace HsnSoft.Base.Json.SystemTextJson.JsonConverters;

public class MaskedConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> MaskedConverters = new();

    public override bool CanConvert(Type typeToConvert)
    {
        var typeMaskingInfo = TypeMaskingInfoHelper.Get(typeToConvert);
        return typeMaskingInfo.HasMaskedProperties;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var maskedConverter = MaskedConverters.GetOrAdd(typeToConvert, CreateMaskedConverter);
        return maskedConverter;
    }

    private JsonConverter CreateMaskedConverter(Type typeToConvert)
    {
        var genericType = typeof(MaskedConverter<>).MakeGenericType(typeToConvert);
        var maskedConverter = (JsonConverter)Activator.CreateInstance(genericType);
        return maskedConverter;
    }
}