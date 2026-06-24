using Hhs.IdentityService.Domain.TenantDomain.Entities;

namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class SubscriptionConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(Subscription.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "Subscriptions";
    public const int SettingsJsonMaxLength = 5000;
}