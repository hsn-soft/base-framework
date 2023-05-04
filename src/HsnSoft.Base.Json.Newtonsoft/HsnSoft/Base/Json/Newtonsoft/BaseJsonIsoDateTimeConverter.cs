using System;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HsnSoft.Base.Json.Newtonsoft;

public class BaseJsonIsoDateTimeConverter : IsoDateTimeConverter, ITransientDependency
{
    private readonly IClock _clock;

    public BaseJsonIsoDateTimeConverter(IClock clock, IOptions<BaseJsonOptions> baseJsonOptions)
    {
        _clock = clock;

        if (baseJsonOptions.Value.DefaultDateTimeFormat != null)
        {
            DateTimeFormat = baseJsonOptions.Value.DefaultDateTimeFormat;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
        {
            return true;
        }

        return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var date = base.ReadJson(reader, objectType, existingValue, serializer) as DateTime?;

        if (date.HasValue)
        {
            return _clock.Normalize(date.Value);
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var date = value as DateTime?;
        base.WriteJson(writer, date.HasValue ? _clock.Normalize(date.Value) : value, serializer);
    }
}