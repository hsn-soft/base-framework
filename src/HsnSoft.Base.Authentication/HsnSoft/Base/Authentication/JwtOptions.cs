namespace HsnSoft.Base.Authentication;

/// <summary>Token validation settings; section <c>Jwt</c>. The auth server that issues the tokens reads the same section.</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC signing key, at least 32 characters. Keep the real one in a secret store, never in appsettings.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Lifetime of an access token; used by whoever issues tokens.</summary>
    public int AccessTokenMinutes { get; set; } = 15;
}
