using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HsnSoft.Base.Json.Mask.Attributes;
using HsnSoft.Base.Json.Mask.MaskingInfo;
using HsnSoft.Base.Json.SystemTextJson.JsonConverters;

namespace HsnSoft.Base.Json.SystemTextJson.Mask;

public static class MaskedSerializationHelper
{
    public static void SetupOptionsForMaskedSerialization(JsonSerializerOptions options)
    {
        var resolver = options.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        if (resolver is DefaultJsonTypeInfoResolver defaultJsonTypeInfoResolver)
            defaultJsonTypeInfoResolver.Modifiers.Add(DetectMaskedMemberAttribute);
        else
            throw new InvalidOperationException($"Expected resolver of type {typeof(DefaultJsonTypeInfoResolver)}");
    }

    private static void DetectMaskedMemberAttribute(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var typeMaskingInfo = TypeMaskingInfoHelper.Get(typeInfo.Type);
        if (typeMaskingInfo.HasMaskedProperties == false)
            return;

        foreach (var propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.AttributeProvider is { } provider &&
                provider.IsDefined(typeof(SensitiveDataAttribute), inherit: true))
            {
                propertyInfo.CustomConverter = new MaskedConverterFactory();
            }
        }
    }

    public static JsonSerializerOptions GetOptionsForMaskedSerialization()
    {
        var settings = new JsonSerializerOptions();
        SetupOptionsForMaskedSerialization(settings);

        return settings;
    }

    public static string SerializeWithMasking(object? value)
    {
        var settings = GetOptionsForMaskedSerialization();
        var serialized = JsonSerializer.Serialize(value, settings);

        return serialized;
    }
}