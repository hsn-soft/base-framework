namespace HsnSoft.Base.RateLimiting;

/// <summary>Who an endpoint's limit counts: each key has a count of its own.</summary>
public enum RateLimitBy
{
    /// <summary>The address of the caller (after the proxies it believes, <c>UseBaseForwardedHeaders</c>): for an endpoint that is open (a sign-in).</summary>
    Address = 0,

    /// <summary>The signed-in user; a caller who is not signed in is counted by its address.</summary>
    User = 1,
}
