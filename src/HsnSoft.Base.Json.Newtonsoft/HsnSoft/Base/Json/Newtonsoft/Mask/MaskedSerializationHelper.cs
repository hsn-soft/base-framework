using HsnSoft.Base.Json.Newtonsoft.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HsnSoft.Base.Json.Newtonsoft.Mask;

public static class MaskedSerializationHelper
{
    public static IContractResolver MaskedContractResolver { get; } = new MaskedContractResolver();

    public static void SetupSettingsForMaskedSerialization(JsonSerializerSettings settings)
    {
        settings.ContractResolver = MaskedContractResolver;
    }

    public static JsonSerializerSettings GetSettingsForMaskedSerialization()
    {
        var settings = new JsonSerializerSettings();
        SetupSettingsForMaskedSerialization(settings);

        return settings;
    }

    public static string SerializeWithMasking(object? value)
    {
        var settings = GetSettingsForMaskedSerialization();
        var serialized = JsonConvert.SerializeObject(value, settings);

        return serialized;
    }
}