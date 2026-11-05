using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Consts;

/// <summary>The fields a list of AppRoleSubscription entities may be sorted by and the sorting when it sends none.</summary>
public static class AppRoleSubscriptionSorting
{
    public const string Default = "id asc";

    public static readonly SortFields<AppRoleSubscription> Fields = new SortFields<AppRoleSubscription>()
        .Add("id", entity => entity.Id);
}
