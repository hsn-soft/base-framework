using System.Collections.Generic;
using Newtonsoft.Json;

namespace HsnSoft.Base.Kafka.Converters;

public static class DefaultJsonOptions
{
    public static JsonSerializerSettings Get()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new TimeSpanConverter(),
            }
        };
        return settings;
    }
}