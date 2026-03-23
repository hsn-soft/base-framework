using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IMayHaveCreator
{
    Guid? CreatorId { get; }
}
