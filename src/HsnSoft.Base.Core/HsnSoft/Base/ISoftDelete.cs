namespace HsnSoft.Base;

/// <summary>
/// An entity that is not removed when it is deleted: it is marked as deleted, and the lists no longer show it (the query filter of the persistence;
/// <c>IDataFilter</c> lets a piece of code see the deleted ones too). When and by whom it was deleted is kept by the audit contracts (<c>IHasDeletionTime</c>, <c>IDeletionAuditedObject</c> of <c>HsnSoft.Base.Auditing</c>).
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
}
