using System;
using HsnSoft.Base.Domain.Entities.Events.Distributed;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.MultiTenancy;

[Serializable]
[EventName("base.multi_tenancy.tenant.connection_string.updated")]
public class TenantConnectionStringUpdatedEto : EtoBase
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string ConnectionStringName { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }
}