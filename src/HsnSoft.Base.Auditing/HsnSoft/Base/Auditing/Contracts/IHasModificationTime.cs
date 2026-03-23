using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IHasModificationTime
{
    DateTime LastModificationTime { get;  }
}
