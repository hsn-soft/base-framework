using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.TenantDomain.Consts;

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
