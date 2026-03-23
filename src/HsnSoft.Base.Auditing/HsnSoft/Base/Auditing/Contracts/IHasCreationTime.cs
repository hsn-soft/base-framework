using System;

namespace HsnSoft.Base.Auditing;

public interface IHasCreationTime
{
    DateTime CreationTime { get; }
}
