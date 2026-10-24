namespace Xyz.AuthServer.Host.Services;

/// <summary>Demo accounts from configuration. A real auth server reads hashed passwords from a user store.</summary>
public sealed class AuthOptions
{
    public int RefreshTokenDays { get; set; } = 7;

    public List<UserAccount> Users { get; set; } = [];
}

public sealed class UserAccount
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}
