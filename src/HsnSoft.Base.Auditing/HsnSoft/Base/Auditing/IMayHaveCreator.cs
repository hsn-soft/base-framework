using System;

namespace HsnSoft.Base.Auditing;

/// <summary>Shows who made a record: the id of the user, <see langword="null"/> when nobody signed in did (a worker, a seed).</summary>
public interface IMayHaveCreator
{
    Guid? CreatedBy { get; }
}
