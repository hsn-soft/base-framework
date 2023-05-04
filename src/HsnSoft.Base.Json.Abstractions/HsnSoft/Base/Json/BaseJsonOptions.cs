using HsnSoft.Base.Collections;

namespace HsnSoft.Base.Json;

public class BaseJsonOptions
{
    public BaseJsonOptions()
    {
        Providers = new TypeList<IJsonSerializerProvider>();
        UseHybridSerializer = true;
    }

    /// <summary>
    /// Used to set default value for the DateTimeFormat.
    /// </summary>
    public string DefaultDateTimeFormat { get; set; }

    /// <summary>
    /// It will try to use System.Json.Text to handle JSON if it can otherwise use Newtonsoft.
    /// Affects both BaseJsonModule and BaseAspNetCoreMvcModule.
    /// See <see cref="BaseSystemTextJsonUnsupportedTypeMatcher"/>
    /// </summary>
    public bool UseHybridSerializer { get; set; }

    public ITypeList<IJsonSerializerProvider> Providers { get; }
}