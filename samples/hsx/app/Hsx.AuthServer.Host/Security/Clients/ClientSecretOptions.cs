namespace Hsx.AuthServer.Security.Clients;

/// <summary>The hash of one secret of a client, and when it stops being accepted (nothing: it does not).</summary>
public sealed class ClientSecretOptions
{
    /// <summary>The SHA-256 of the secret, 64 hex digits. The secret is a random one of at least 32 bytes, made for the client; a hash of a password a person chose is not what this is for.</summary>
    public string Hash { get; set; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; set; }
}
