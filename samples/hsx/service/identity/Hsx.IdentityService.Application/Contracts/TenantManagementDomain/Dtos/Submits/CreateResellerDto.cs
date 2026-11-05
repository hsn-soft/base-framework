using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.TenantDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos.Submits;

/// <summary>A reseller to be made, with the administrator of its tenant.</summary>
public sealed class CreateResellerDto
{
    [Required]
    [StringLength(TenantConsts.TitleMaxLength)]
    public string? Title { get; set; }

    /// <summary>What the tenant is called in lists and paths (small letters, unique).</summary>
    [Required]
    [StringLength(TenantConsts.NameMaxLength)]
    public string? Name { get; set; }

    [Required]
    public AdminUserInputDto? AdminUser { get; set; }
}
