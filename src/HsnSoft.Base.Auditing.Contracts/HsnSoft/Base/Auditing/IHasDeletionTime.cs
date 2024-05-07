using System;

namespace HsnSoft.Base.Auditing;

public interface IHasDeletionTime : ISoftDelete
{
    DateTime? DeletionTime { get; set; }
}