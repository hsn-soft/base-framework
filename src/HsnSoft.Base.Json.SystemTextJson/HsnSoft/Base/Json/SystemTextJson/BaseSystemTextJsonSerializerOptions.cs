using System.Text.Json;
using HsnSoft.Base.Collections;

namespace HsnSoft.Base.Json.SystemTextJson;

public class BaseSystemTextJsonSerializerOptions
{
    public BaseSystemTextJsonSerializerOptions()
    {
        JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        UnsupportedTypes = new TypeList();
    }

    public JsonSerializerOptions JsonSerializerOptions { get; }

    public ITypeList UnsupportedTypes { get; }
}