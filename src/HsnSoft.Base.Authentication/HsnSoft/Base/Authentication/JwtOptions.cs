using System.Collections.Generic;

namespace HsnSoft.Base.Authentication;

/// <summary>Token validation settings; section <c>Jwt</c>. The auth server that issues the tokens reads the same section.</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The audience of a host that checks tokens: the name of this service (<c>abc-order</c>). The <c>aud</c> claim of a token may be a list; the token is accepted when the list has this audience, so a client whose token does not name a
    /// service cannot call it.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>The one that issues tokens: the audiences it puts into the <c>aud</c> claim of a token (the services its clients may reach) when it is not told others; a host that only checks tokens leaves it empty.</summary>
    public List<string> IssuedAudiences { get; set; } = [];

    /// <summary>
    /// Whether the issuer of a token is checked against <see cref="Issuer"/>. Not set: it is checked in every environment except Development (a developer's machine may have a token of another auth server); set it to true or false
    /// to say it for an environment.
    /// </summary>
    public bool? ValidateIssuer { get; set; }

    /// <summary>The difference between the clocks of two machines that is forgiven when the lifetime of a token is checked, in seconds. Default: 30.</summary>
    public int ClockSkewSeconds { get; set; } = 30;

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
