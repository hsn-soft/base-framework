using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;

public class CustomerConfiguration : CreationAuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; }
    public string NormalizerProvider { get; set; }
    public bool IsDeleted { get; set; }
    public ClientNormalizerSetting NormalizerSetting { get; private set; }

    private CustomerConfiguration()
    {
        NormalizerProvider = String.Empty;
    }

    internal CustomerConfiguration(Guid id, Guid tenantId, Guid clientId, string clientName, string normalizerProvider,
        ClientNormalizerSetting normalizerSetting) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetClientId(clientId);
        ClientName = clientName;
        NormalizerProvider = normalizerProvider;
        NormalizerSetting = normalizerSetting;
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
}

public class ClientNormalizerSetting
{
    public bool IsScrapingOperationActive { get; set; }

    public bool IsOutlineOperationActive { get; set; }
    public TextNormalizeProviderTypes ContentOutlineProvider { get; set; }
    public string ContentOutlinePrompt { get; set; }
    public TextNormalizeProviderTypes AnalysisOutlineProvider { get; set; }
    public string AnalysisOutlineContentPrompt { get; set; }
    public string AnalysisOutlineIntroPrompt { get; set; }
    public string AnalysisOutlineOutroPrompt { get; set; }
    public bool IsForceContentDetailInAnalyseActive { get; set; }
}