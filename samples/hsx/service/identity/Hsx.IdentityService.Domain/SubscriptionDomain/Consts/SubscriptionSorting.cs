using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Consts;

/// <summary>The fields a list of subscriptions may be sorted by and the sorting when it sends none.</summary>
public static class SubscriptionSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<Subscription> Fields = new SortFields<Subscription>()
        .Add("validFrom", entity => entity.ValidFrom)
        .Add("createdAt", entity => entity.CreatedAt);
}
