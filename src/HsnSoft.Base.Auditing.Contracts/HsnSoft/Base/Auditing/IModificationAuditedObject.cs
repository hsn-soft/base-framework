using System;

namespace HsnSoft.Base.Auditing;

public interface IModificationAuditedObject : IHasModificationTime
{
    Guid? LastModifierId { get; set; }
}