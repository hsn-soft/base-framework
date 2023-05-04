using System;
using HsnSoft.Base.Domain.Entities.Events.Distributed;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.MultiTenancy;

[Serializable]
[EventName("base.multi_tenancy.tenant.created")]
public class TenantCreatedEto : EtoBase
{
    public Guid Id { get; set; }

    public string Name { get; set; }
}