using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

public sealed class AppRoleCreateDto
{
    [Required]
    [StringLength(AppRoleConsts.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>
    /// Which subscriptions its users see, and so which data: <c>System</c> (the roles of the system tenant), <c>Tenant</c> (all the sales of the tenant), <c>Company</c> or <c>Customer</c> (the sales of the tenant for the company or the customer it names). It cannot be changed: another type is another role.
    /// The role is given the sales that match it, and every later sale that matches it.
    /// </summary>
    [Required]
    public AppRoleType? RoleType { get; set; }

    /// <summary>The company of a <c>Company</c> role (one of the companies in the subscriptions of the tenant); left out for any other type.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>The customer of a <c>Customer</c> role (one of the customers in the subscriptions of the tenant); left out for any other type.</summary>
    public Guid? CustomerId { get; set; }
}

public sealed class AppRoleUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(AppRoleConsts.NameMaxLength)]
    public string? Name { get; set; }
}
