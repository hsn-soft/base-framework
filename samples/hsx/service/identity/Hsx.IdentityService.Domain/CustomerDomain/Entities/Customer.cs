using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.CustomerDomain.Consts;

namespace Hsx.IdentityService.Domain.CustomerDomain.Entities;

/// <summary>
/// A customer of a company, known by its domain (<c>abc.example.com</c>). The data of a customer is shown to a tenant by the subscriptions it sold to the customer.
/// </summary>
public sealed class Customer : FullAuditedEntity<Guid>
{
    // for EF Core
    private Customer()
    {
    }

    public Guid CompanyId { get; private set; }

    public string Domain { get; private set; } = string.Empty;
    public string NormalizedDomain { get; private set; } = string.Empty;

    public static Customer Create(Guid companyId, string domain)
    {
        Customer entity = new()
        {
            Id = Guid.CreateVersion7(),
            CompanyId = Guard.NotDefault<Guid>(companyId, nameof(CompanyId)),
            Domain = Guard.NotNullOrWhiteSpace(domain, nameof(Domain), CustomerConsts.DomainMaxLength).Trim()
        };
        entity.NormalizedDomain = StringHelper.Normalize(entity.Domain);
        return entity;
    }
}
