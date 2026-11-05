using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

/// <summary>A permission of the catalogue (the code of the system is its only author: it is read here, never written).</summary>
public sealed class PermissionDto : EntityDto<Guid>
{
    public string Code { get; init; } = string.Empty;

    public PermissionType Type { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}
