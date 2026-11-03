using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

/// <summary>A permission of the catalogue.</summary>
public sealed class PermissionDto : EntityDto<Guid>
{
    public string Code { get; init; } = string.Empty;

    /// <summary><c>Page</c>, <c>Endpoint</c> or <c>Operation</c>.</summary>
    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>A permission that was granted to a role.</summary>
public sealed class GrantedPermissionDto
{
    public string Code { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;
}
