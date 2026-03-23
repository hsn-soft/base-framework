using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IDeletionAuditedObject : IHasDeletionTime
{
    Guid? DeleterId { get;  }
}