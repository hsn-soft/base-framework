using System;

namespace HsnSoft.Base.Auditing;

public interface IDeletionAuditedObject : IHasDeletionTime
{
    Guid? DeleterId { get; set; }
}