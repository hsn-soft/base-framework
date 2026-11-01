using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;

public sealed class TenantCreateDto
{
    [Required]
    [StringLength(TenantConsts.TitleMaxLength)]
    public string? Title { get; set; }

    [Required]
    [StringLength(TenantConsts.NameMaxLength)]
    public string? Name { get; set; }

    [Required]
    public TenantTypes? TenantType { get; set; }

    /// <summary>The tenant it is under; nothing for a tenant at the root (only the system tenant makes one).</summary>
    public Guid? ParentId { get; set; }
}

/// <summary>What can be changed of a tenant (its name stays: it is what it is called in the system).</summary>
public sealed class TenantUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(TenantConsts.TitleMaxLength)]
    public string? Title { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Puts a tenant, with the tenants under it, under another (or at the root).</summary>
public sealed class TenantMoveDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    public Guid? NewParentId { get; set; }
}
