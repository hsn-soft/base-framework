using System.Text.Json;

namespace HsnSoft.Base.Json.SystemTextJson.Mask;

public static class MaskedSerializationExtensions
{
    public static void SetupSettingsForMaskedSerialization(this JsonSerializerOptions options)
    {
        MaskedSerializationHelper.SetupOptionsForMaskedSerialization(options);
    }
}