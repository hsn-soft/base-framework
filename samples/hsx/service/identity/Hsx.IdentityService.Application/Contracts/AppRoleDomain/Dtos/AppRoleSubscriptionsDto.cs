using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

/// <summary>The subscriptions of a role: they follow its type and are kept by the system (they are read, not edited).</summary>
public sealed class AppRoleSubscriptionsDto : IBaseDto
{
    public Guid RoleId { get; init; }

    public List<Guid> SubscriptionIds { get; init; } = [];
}
