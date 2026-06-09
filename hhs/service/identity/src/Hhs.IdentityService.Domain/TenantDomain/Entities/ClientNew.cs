using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;


/// <summary>
/// CINER HOLDING
///     haberturk.com
///     bloomberght.com
/// ILBAK HOLDING
///     cnbce.com
/// </summary>
public sealed class ClientNew : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public Guid HoldingId { get; private set; }

    public Holding Holding { get; private set; } = null!;

    public string Domain { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    private ClientNew()
    {
    }

    public ClientNew(
        Guid id,
        Guid holdingId,
        string domain,
        string displayName)
    {
        Id = id;
        HoldingId = holdingId;
        Domain = domain;
        DisplayName = displayName;
    }
}