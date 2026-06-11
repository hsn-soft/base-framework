using Hhs.IdentityService.Domain.Enums;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class Subscription : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public Guid ResellerTenantId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Company Company { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; }

    public ProductTypes ProductType { get; private set; }

    public DateTime ValidFrom { get; set; }

    public bool IsBlocked { get; private set; }

    public DateTime? ValidTo { get; private set; }

    [NotNull] public string SettingsJson { get; private set; }


    private Subscription()
    {
        // Not-Null string fields
        SettingsJson = "{}";

        // navigation fields
        Company = null;
        Customer = null;
    }

    internal Subscription(
        Guid resellerTenantId,
        Guid companyId,
        Guid customerId,
        ProductTypes productType,
        [NotNull] string settingsJson = "{}"
    ) : this(Guid.CreateVersion7(),
        resellerTenantId: resellerTenantId,
        companyId: companyId,
        customerId: customerId,
        productType: productType,
        settingsJson: settingsJson)
    {
    }

    internal Subscription(Guid id,
        Guid resellerTenantId,
        Guid companyId,
        Guid customerId,
        ProductTypes productType,
        [NotNull] string settingsJson = "{}"
    ) : this()
    {
        Id = id;

        ResellerTenantId = resellerTenantId;
        CompanyId = companyId;
        CustomerId = customerId;
        ProductType = productType;

        ValidFrom = DateTime.UtcNow;

        SetSettingsJson(settingsJson);
    }

    public void SetSettingsJson(string settingsJson)
    {
        SettingsJson = LocalizedModelValidator.NotNullOrWhiteSpace(settingsJson, $"{nameof(Subscription)}:{nameof(SettingsJson)}");
        if (SettingsJson.Length < 2 || !SettingsJson.StartsWith("{") || !SettingsJson.EndsWith("}"))
        {
            throw new ArgumentException($"{nameof(Subscription)}:{nameof(SettingsJson)}");
        }
    }

    public void BlockSubscription(DateTime? validTo = null)
    {
        IsBlocked = true;
        ValidTo = validTo ?? DateTime.UtcNow;
    }

    public void UnBlockSubscription(DateTime? validTo = null)
    {
        IsBlocked = false;
        ValidTo = validTo;
    }
}