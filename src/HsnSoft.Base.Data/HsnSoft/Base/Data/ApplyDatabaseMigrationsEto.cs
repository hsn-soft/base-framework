using System;
using HsnSoft.Base.Domain.Entities.Events.Distributed;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.Data;

[Serializable]
[EventName("base.data.apply_database_migrations")]
public class ApplyDatabaseMigrationsEto : EtoBase
{
    public Guid? TenantId { get; set; }

    public string DatabaseName { get; set; }
}