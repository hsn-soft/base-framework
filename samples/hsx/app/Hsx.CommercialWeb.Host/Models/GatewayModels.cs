namespace Hsx.CommercialWeb.Models;

public sealed class Paged<T>
{
    public List<T> Items { get; init; } = [];

    public int DataTotalCount { get; init; }
}

public sealed class TokenPayload
{
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>How long the token is valid, in seconds: the sign-in of the page lasts as long.</summary>
    public int ExpiresIn { get; init; }
}

public sealed class TenantItem
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string TenantType { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class UserItem
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ContentItem
{
    public Guid Id { get; init; }

    public string ScopeKey { get; init; } = string.Empty;

    public string ContentKey { get; init; } = string.Empty;

    public string SlugKey { get; init; } = string.Empty;

    public string? VideoStatus { get; init; }

    public string? CurrentMilestone { get; init; }
}
