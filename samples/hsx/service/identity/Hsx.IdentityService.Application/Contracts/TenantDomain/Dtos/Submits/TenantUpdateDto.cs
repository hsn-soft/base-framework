using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.TenantDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;

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
