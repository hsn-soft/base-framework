using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;

/// <summary>Puts a tenant, with the tenants under it, under another (or at the root).</summary>
public sealed class TenantMoveDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    public Guid? NewParentId { get; set; }
}
