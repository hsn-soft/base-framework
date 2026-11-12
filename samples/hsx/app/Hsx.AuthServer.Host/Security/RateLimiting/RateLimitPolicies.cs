namespace Hsx.AuthServer.Security.RateLimiting;

/// <summary>The names of the policies an endpoint asks for with <c>[EnableRateLimiting]</c>.</summary>
public static class RateLimitPolicies
{
    public const string SignIn = "sign-in";

    public const string Refresh = "refresh";
}
