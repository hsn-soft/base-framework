using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Hsx.CommercialWeb.Host.Services;

/// <summary>Reads what the access token says about the signed-in user, to show it (the page does not trust it for anything: the services check the token).</summary>
public static class TokenClaims
{
    /// <summary>The values of a claim of the token, by name (<c>allowed_tenant_id</c>, <c>allowed_scope_key</c>, ...); a claim with one value is a list of one.</summary>
    public static IReadOnlyList<string> Read(string? token, string claim)
    {
        string[] parts = token?.Split('.') ?? [];
        if (parts.Length != 3)
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(Base64UrlTextEncoder.Decode(parts[1]));
        if (!document.RootElement.TryGetProperty(claim, out JsonElement value))
        {
            return [];
        }

        return value.ValueKind == JsonValueKind.Array ? [.. value.EnumerateArray().Select(item => item.ToString())] : [value.ToString()];
    }
}
