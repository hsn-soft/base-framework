using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HsnSoft.Base.Authentication;
using Microsoft.Extensions.Options;

namespace Hsx.AuthServer.Host.Security.Clients;

/// <summary>
/// The clients of the auth server, kept in memory from the section <c>Auth:Clients</c> and checked when the host starts (a setting that cannot be right stops it, with a message that names the client): an id is not empty and not repeated, a client that needs a secret has one,
/// a hash is a SHA-256 in hex, an audience is one of <c>Jwt:IssuedAudiences</c>, and a client that may ask for a token for itself has a secret. A secret is compared in constant time and a secret that has expired is not accepted.
/// </summary>
public sealed class ClientRegistry
{
    public const string SectionName = "Auth:Clients";

    // checked when the client does not exist, so an unknown id takes as long as a wrong secret
    private static readonly byte[] s_unknownHash = SHA256.HashData(Encoding.UTF8.GetBytes("not-the-secret-of-any-client"));

    private readonly Dictionary<string, ClientOptions> _clients;
    private readonly TimeProvider _time;

    public ClientRegistry(IConfiguration configuration, IOptions<JwtOptions> jwt, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(jwt);

        _time = time ?? throw new ArgumentNullException(nameof(time));
        List<ClientOptions> clients = configuration.GetSection(SectionName).Get<List<ClientOptions>>() ?? [];
        _clients = new Dictionary<string, ClientOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (ClientOptions client in clients)
        {
            Validate(client, jwt.Value.IssuedAudiences);
            if (!_clients.TryAdd(client.ClientId, client))
            {
                throw new InvalidOperationException($"{SectionName}: the client '{client.ClientId}' is listed twice.");
            }
        }
    }

    /// <summary>The client with this id when it is there and enabled.</summary>
    public ClientOptions? Find(string? clientId)
        => !string.IsNullOrWhiteSpace(clientId) && _clients.TryGetValue(clientId.Trim(), out ClientOptions? client) && client.Enabled ? client : null;

    /// <summary>Whether the client is who it says: a client that needs no secret is its name only, any other one has to give a secret that one of its hashes (not expired) matches. <paramref name="client"/> is <see langword="null"/> for an id that is not known: the answer is still the time of a check.</summary>
    public bool Proves(ClientOptions? client, string? secret)
    {
        byte[] given = SHA256.HashData(Encoding.UTF8.GetBytes(secret ?? string.Empty));
        bool matches = false;
        DateTimeOffset now = _time.GetUtcNow();
        foreach (ClientSecretOptions candidate in client?.SecretHashes ?? [])
        {
            // every hash is compared, so how many there are, and which one is the right one, cannot be told from the time
            bool same = CryptographicOperations.FixedTimeEquals(given, Convert.FromHexString(candidate.Hash));
            matches |= same && (candidate.ExpiresAt is not { } end || end > now);
        }

        if (client is null)
        {
            _ = CryptographicOperations.FixedTimeEquals(given, s_unknownHash);
            return false;
        }

        return !client.RequireSecret || matches;
    }

    private static void Validate(ClientOptions client, IReadOnlyCollection<string> issuedAudiences)
    {
        string where = $"{SectionName}: the client '{client.ClientId}'";
        if (string.IsNullOrWhiteSpace(client.ClientId))
        {
            throw new InvalidOperationException($"{SectionName}: a client has no ClientId.");
        }

        if (client.Audiences.Count == 0)
        {
            throw new InvalidOperationException($"{where} has no audience: a token with none is accepted by no service.");
        }

        if (client.Audiences.FirstOrDefault(audience => !issuedAudiences.Contains(audience)) is { } unknown)
        {
            throw new InvalidOperationException($"{where} has the audience '{unknown}', which is not one of Jwt:IssuedAudiences.");
        }

        if (client.RequireSecret && client.SecretHashes.Count == 0)
        {
            throw new InvalidOperationException($"{where} needs a secret (RequireSecret) and has no SecretHashes.");
        }

        if (client.AllowClientCredentials && !client.RequireSecret)
        {
            throw new InvalidOperationException($"{where} may ask for a token for itself and has no secret: a machine is known by its secret only.");
        }

        if (!client.AllowUserSignIn && !client.AllowClientCredentials)
        {
            throw new InvalidOperationException($"{where} may neither sign a user in nor ask for a token for itself.");
        }

        foreach (ClientSecretOptions secret in client.SecretHashes)
        {
            if (secret.Hash.Length != 64 || !secret.Hash.All(Uri.IsHexDigit))
            {
                throw new InvalidOperationException($"{where} has a secret hash that is not a SHA-256 in hex (64 digits).");
            }
        }

        if (client.RefreshTokenInBody && !client.AllowOfflineAccess)
        {
            throw new InvalidOperationException($"{where} keeps the refresh token in the body (RefreshTokenInBody) and does not allow a refresh token (AllowOfflineAccess).");
        }

        if (client.AccessTokenMinutes < 0)
        {
            throw new InvalidOperationException($"{where} has AccessTokenMinutes below zero ({client.AccessTokenMinutes.ToString(CultureInfo.InvariantCulture)}).");
        }
    }
}
