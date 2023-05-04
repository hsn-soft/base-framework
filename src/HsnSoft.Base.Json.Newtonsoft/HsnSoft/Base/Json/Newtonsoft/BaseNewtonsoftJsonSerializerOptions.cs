using HsnSoft.Base.Collections;
using Newtonsoft.Json;

namespace HsnSoft.Base.Json.Newtonsoft;

public class BaseNewtonsoftJsonSerializerOptions
{
    public BaseNewtonsoftJsonSerializerOptions()
    {
        Converters = new TypeList<JsonConverter>();
    }

    public ITypeList<JsonConverter> Converters { get; }
}