using System;

namespace HsnSoft.Base.Auditing;

public interface IHasModificationTime
{
    DateTime? LastModificationTime { get; set; }
}
