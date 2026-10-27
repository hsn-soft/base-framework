namespace HsnSoft.Base.Auditing;

/// <summary>Shows everything about a record: when it was made, last changed and deleted, and by whom.</summary>
public interface IFullAuditedObject : IAuditedObject, IDeletionAuditedObject;
