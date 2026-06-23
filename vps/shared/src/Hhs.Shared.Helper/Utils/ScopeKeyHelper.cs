using Hhs.Shared.Helper.Enums;

namespace Hhs.Shared.Helper.Utils;

public static class ScopeKeyHelper
{
    public static string Generate(Guid customerId, ProductTypes productType)
    {
        if (customerId == Guid.Empty || productType == ProductTypes.Unknown)
        {
            throw new ArgumentException($"{nameof(productType)} is unknown", nameof(productType));
        }

        return $"{customerId:N}:{productType.ToPrompt()}";
    }
}