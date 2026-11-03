using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Entities;

/// <summary>
/// A product that a reseller tenant sold to a customer: it belongs to that tenant (<see cref="TenantId"/>), which sees it with the tenants under it. Its scope key (<c>{customerId}:{product}</c>) is what the data of the product is shown by.
/// </summary>
public sealed class Subscription : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private Subscription()
    {
    }

    /// <summary>The reseller that sold it.</summary>
    public Guid TenantId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public ProductTypes ProductType { get; private set; }

    public string ScopeKey { get; private set; } = string.Empty;

    public DateTimeOffset ValidFrom { get; private set; }

    public DateTimeOffset? ValidTo { get; private set; }

    public bool IsBlocked { get; private set; }

    public string? SettingsJson { get; private set; }

    public static Subscription Create(Guid tenantId, Guid companyId, Guid customerId, ProductTypes productType, DateTimeOffset? validTo, bool isBlocked, string? settingsJson)
    {
        Subscription entity = new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guard.NotDefault<Guid>(tenantId, nameof(TenantId)),
            CompanyId = Guard.NotDefault<Guid>(companyId, nameof(CompanyId)),
            CustomerId = Guard.NotDefault<Guid>(customerId, nameof(CustomerId)),
            ProductType = productType,
            ScopeKey = ScopeKeyHelper.Generate(customerId, productType),
            ValidFrom = DateTimeOffset.UtcNow
        };
        entity.Update(validTo, isBlocked, settingsJson);
        return entity;
    }

    public void Update(DateTimeOffset? validTo, bool isBlocked, string? settingsJson)
    {
        ValidTo = validTo;
        IsBlocked = isBlocked;
        SettingsJson = Optional(settingsJson, nameof(SettingsJson), SubscriptionConsts.SettingsJsonMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
