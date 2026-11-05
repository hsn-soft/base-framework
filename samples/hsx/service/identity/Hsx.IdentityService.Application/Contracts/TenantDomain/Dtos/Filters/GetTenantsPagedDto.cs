using System.ComponentModel.DataAnnotations;
using Hsx.IdentityService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;

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
