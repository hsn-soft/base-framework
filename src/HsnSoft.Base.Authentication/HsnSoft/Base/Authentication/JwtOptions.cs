namespace HsnSoft.Base.Authentication;

/// <summary>Token validation settings; section <c>Jwt</c>. The auth server that issues the tokens reads the same section.</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC signing key (symmetric), at least 32 characters; every host that checks a token has the same one as the one that signs. Keep the real one in a secret store, never in appsettings.
    /// Ignored when <see cref="PublicKeyFile"/> is given.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The RSA <b>public</b> key (a file: the XML of <c>RSA.ToXmlString</c> or a PEM, relative to where the application runs, or absolute) that tokens are checked with: they are signed
    /// with the private key (RS256) that only the auth server has, so a host that checks tokens cannot make one. When it is given, <see cref="Key"/> is not used.
    /// </summary>
    public string? PublicKeyFile { get; set; }

    /// <summary>The RSA <b>private</b> key (a file, as <see cref="PublicKeyFile"/>) that signs the tokens; only the one that issues tokens (the auth server) has and reads it.</summary>
    public string? PrivateKeyFile { get; set; }

    /// <summary>Lifetime of an access token; used by whoever issues tokens.</summary>
    public int AccessTokenMinutes { get; set; } = 15;
}
