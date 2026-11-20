namespace Hsx.AuthServer.Security;

/// <summary>Where a sign-in came from, for the audit.</summary>
public sealed record SignInOrigin(string? IpAddress, string? UserAgent);
