using System;
using HsnSoft.Base.Json.Mask.Consts;
using Newtonsoft.Json;

namespace HsnSoft.Base.Json.Newtonsoft.Converters;

public class MaskConverter : JsonConverter
{
    public override bool CanWrite => true;

    public override bool CanRead => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(MaskStrings.Default);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}