using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.FeedRService.Domain.ConfigurationDomain.Entities;

public class CustomerConfiguration : CreationAuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; }

    public string Network { get; private set; }

    public string AdUnitName { get; private set; }

    public string AdUnitIdTopLevel { get; private set; }

    public List<string> AdUnitId { get; private set; }

    private CustomerConfiguration()
    {
        ClientName = string.Empty;
    }

    public CustomerConfiguration(Guid id, Guid tenantId, Guid clientId, string clientName, string network, string adUnitName, string adUnitIdTopLevel, List<string> adUnitId) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetClientId(clientId);
        ClientName = clientName;
        Network = network;
        AdUnitName = adUnitName;
        AdUnitIdTopLevel = adUnitIdTopLevel ?? string.Empty;
        AdUnitId = adUnitId;
    }

    internal void SetId(Guid id)
    {
        if (Id == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(Id)} is invalid", nameof(id));
        }

        Id = id;
    }

    internal void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    public void Update(string clientName, string network, string adUnitName, string adUnitIdTopLevel, List<string> adUnitId)
    {
        ClientName = clientName;
        Network = network;
        AdUnitName = adUnitName;
        AdUnitIdTopLevel = adUnitIdTopLevel ?? string.Empty;
        AdUnitId = adUnitId;
    }
}

