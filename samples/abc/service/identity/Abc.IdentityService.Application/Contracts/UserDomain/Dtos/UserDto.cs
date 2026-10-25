namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos;

public sealed class UserDto
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Language { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsActive { get; init; }

    public Guid TenantId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public Guid? CreatedBy { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public Guid? UpdatedBy { get; init; }
}
