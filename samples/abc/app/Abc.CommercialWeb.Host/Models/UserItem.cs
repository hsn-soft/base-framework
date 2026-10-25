namespace Abc.CommercialWeb.Host.Models;

public sealed class UserItem
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class PagedUsers
{
    public List<UserItem> Items { get; init; } = [];

    public int DataTotalCount { get; init; }
}

public sealed class TokenPayload
{
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>How long the token is valid, in seconds: the sign-in of the page lasts as long.</summary>
    public int ExpiresIn { get; init; }
}

public sealed class LoginModel
{
    /// <summary>The sign-in ended (the token expired or the gateway did not accept it): the page says so.</summary>
    public bool Expired { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}

public sealed class UsersModel
{
    public string? Search { get; set; }

    public PagedUsers Users { get; set; } = new();
}
