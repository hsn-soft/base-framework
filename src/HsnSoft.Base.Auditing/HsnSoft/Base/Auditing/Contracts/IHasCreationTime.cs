using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IHasCreationTime
{
    DateTime CreationTime { get; }
}
