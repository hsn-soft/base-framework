using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class Customer : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public Guid CompanyId { get; private set; }

    [CanBeNull] public Company Company { get; set; }

    [NotNull] public string Domain { get; private set; }

    [NotNull] public string NormalizedDomain { get; private set; }

    private Customer()
    {
        // Not-Null string fields
        Domain = string.Empty;
        NormalizedDomain = string.Empty;

        // navigation fields
        Company = null;
    }

    internal Customer(
        Guid companyId,
        [NotNull] string domain
    ) : this(Guid.CreateVersion7(), companyId: companyId, domain: domain)
    {
    }

    internal Customer(Guid id,
        Guid companyId,
        [NotNull] string domain
    ) : this()
    {
        Id = id;
        CompanyId = companyId;

        SetDomain(domain);
    }

    internal void SetDomain(string domain)
    {
        string checkDomain = LocalizedModelValidator.NotNullOrWhiteSpace(domain, $"{nameof(Customer)}:{nameof(Domain)}", CustomerConsts.DomainMaxLength);
        Domain = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomain));
        NormalizedDomain = StringHelper.Normalize(Domain);
    }
}