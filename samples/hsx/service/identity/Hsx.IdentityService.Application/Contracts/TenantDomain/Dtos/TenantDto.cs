using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos;

public sealed class TenantDto : AuditedEntityDto<Guid>
{
    public Guid? ParentId { get; init; }

    /// <summary>Where the tenant is in the tree: <c>/{root}/{parent}/{tenant}/</c>.</summary>
    public string NormalizedAccessPath { get; init; } = string.Empty;

    public TenantTypes TenantType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
