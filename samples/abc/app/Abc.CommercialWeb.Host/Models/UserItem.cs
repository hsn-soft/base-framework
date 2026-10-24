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

    public int TotalCount { get; init; }
}

public sealed class TokenPayload
{
    public string AccessToken { get; init; } = string.Empty;
}

public sealed class LoginModel
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}

public sealed class UsersModel
{
    public string? Search { get; set; }

    public PagedUsers Users { get; set; } = new();
}
