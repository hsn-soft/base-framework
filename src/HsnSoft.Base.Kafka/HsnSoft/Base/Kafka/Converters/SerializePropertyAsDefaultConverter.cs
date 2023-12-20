using System;
using Newtonsoft.Json;

namespace HsnSoft.Base.Kafka.Converters;

/// <summary>
/// https://stackoverflow.com/questions/59828937/exclude-an-enum-property-of-a-model-from-using-the-jsonstringenumconverter-which
/// Apply this converter to a property to force the property to be serialized with default options.
/// This converter can ONLY be applied to a property; setting it in options or on a type may cause a stack overflow exception!
/// </summary>
/// <typeparam name="T">the property's declared return type</typeparam>
public class SerializePropertyAsDefaultConverter<T> : JsonConverter<T>
{
    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<T>(reader);
    }
}