using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IModificationAuditedObject : IHasModificationTime
{
    Guid? LastModifierId { get; }
}