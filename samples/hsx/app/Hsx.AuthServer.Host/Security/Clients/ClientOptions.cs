namespace Hsx.AuthServer.Host.Security.Clients;

/// <summary>
/// A client of the auth server (an application that asks for a token): the back office, the commercial web, a public application that reads static data. The list is the section <c>Auth:Clients</c> of the settings, kept in memory; the secrets are never here, only the hash of each (<c>SecretHashes</c>:
/// the SHA-256 of a random secret, in hex), and the real hashes come from the environment or the secret store of the deployment (ADR-0018).
/// </summary>
public sealed class ClientOptions
{
    /// <summary>What the client sends to name itself; unique (compared without regard to case).</summary>
    public string ClientId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>Whether the client has to prove itself with a secret. A public client (a page or an app that cannot keep a secret) does not, and is then only named: what it may do is its audiences, and a user still signs in with a password.</summary>
    public bool RequireSecret { get; set; } = true;

    /// <summary>The hashes of the secrets of the client; more than one while a secret is being changed (the old one has an <c>ExpiresAt</c>).</summary>
    public List<ClientSecretOptions> SecretHashes { get; set; } = [];

    /// <summary>The services a token of this client may reach (<c>aud</c>): each is one of <c>Jwt:IssuedAudiences</c>. A request may ask for fewer, never for more.</summary>
    public List<string> Audiences { get; set; } = [];

    /// <summary>Whether a user may sign in through this client (the grant <c>password</c> of the previous server).</summary>
    public bool AllowUserSignIn { get; set; } = true;

    /// <summary>Whether the client may ask for a token for itself with its secret (<c>client_credentials</c>: a machine, no user). Needs a secret.</summary>
    public bool AllowClientCredentials { get; set; }

    /// <summary>Whether a user who signs in through this client may ask for a refresh token (the cookie).</summary>
    public bool AllowOfflineAccess { get; set; }

    /// <summary>
    /// Where the refresh token of a user who signs in through this client travels: <see langword="false"/> in an HttpOnly cookie (a browser: the page never sees it), <see langword="true"/> in the body of the answer and of the next request (a mobile application, which keeps it in its secure
    /// storage and has no cookie jar to rely on). Needs <see cref="AllowOfflineAccess"/>. A client has one way, never both.
    /// </summary>
    public bool RefreshTokenInBody { get; set; }

    /// <summary>How long the access token lives, in minutes; 0 is the value of <c>Jwt:AccessTokenMinutes</c>.</summary>
    public int AccessTokenMinutes { get; set; }
}
