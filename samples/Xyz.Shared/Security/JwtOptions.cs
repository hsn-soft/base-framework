namespace Xyz.Shared.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "xyz-auth";

    public string Audience { get; set; } = "xyz";

    /// <summary>HMAC signing key, at least 32 characters. Keep the real one in a secret store, never in appsettings.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}

public static class Roles
{
    public const string Admin = "admin";
    public const string Customer = "customer";
}
