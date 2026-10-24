namespace Abc.AuthServer.Host.Options;

/// <summary>The users the sample knows (section <c>Auth</c>). A real auth server asks a store; the sample keeps a short list so the other hosts have someone to log in as.</summary>
public sealed class AuthOptions
{
    public List<UserAccount> Users { get; set; } = [];
}

public sealed class UserAccount
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}
