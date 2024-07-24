using Newtonsoft.Json;

namespace HsnSoft.Base.Json.Newtonsoft.Mask;

public static class MaskedSerializationExtensions
{
    public static void SetupSettingsForMaskedSerialization(this JsonSerializerSettings settings)
    {
        MaskedSerializationHelper.SetupSettingsForMaskedSerialization(settings);
    }
}