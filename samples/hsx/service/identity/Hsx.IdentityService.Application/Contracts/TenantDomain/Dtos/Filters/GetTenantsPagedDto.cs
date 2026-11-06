using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Filters;

public sealed class GetTenantsPagedDto : PagedDataRequestDto
{
    public TenantTypes? TenantType { get; set; }

    /// <summary>Only the tenants directly under this one.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>A part of the name.</summary>
    [StringLength(100)]
    public string? Name { get; set; }
}
