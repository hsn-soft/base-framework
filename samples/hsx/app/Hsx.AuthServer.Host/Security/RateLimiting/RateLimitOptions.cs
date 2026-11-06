namespace Hsx.AuthServer.Host.Security.RateLimiting;

/// <summary>
/// The section <c>Auth:RateLimits</c>: how many requests one address may make in a minute (ADR-0018). The address is the one the proxies of <c>Hosting:ForwardedHeaders</c> say the caller has. The limits are of one instance: a deployment with several pods lets each count its own.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "Auth:RateLimits";

    /// <summary>Everything but the health endpoints, for one address.</summary>
    public int GlobalPerMinute { get; set; } = 300;

    /// <summary>The login and the token of a client (a password or a secret is guessed here): the strictest.</summary>
    public int SignInPerMinute { get; set; } = 10;

    /// <summary>The refresh of an access token.</summary>
    public int RefreshPerMinute { get; set; } = 30;
}
