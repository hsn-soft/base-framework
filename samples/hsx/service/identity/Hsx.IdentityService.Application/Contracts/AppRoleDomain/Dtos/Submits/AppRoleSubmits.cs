using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

public sealed class AppRoleCreateDto
{
    [Required]
    [StringLength(AppRoleConsts.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>The subscriptions the role has: what its users may see of the products.</summary>
    public List<Guid>? SubscriptionIds { get; set; }
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

/// <summary>Gives a role exactly these subscriptions (ones the tenant of the caller, or a tenant under it, sold).</summary>
public sealed class AppRoleSetSubscriptionsDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    public List<Guid>? SubscriptionIds { get; set; }
}
