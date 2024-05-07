using System;

namespace HsnSoft.Base.Auditing;

public interface IMayHaveCreator
{
    Guid? CreatorId { get; }
}
