using System;

namespace HsnSoft.Base.Auditing.Contracts;

public interface IHasDeletionTime : ISoftDelete
{
    DateTime? DeletionTime { get;  }
}