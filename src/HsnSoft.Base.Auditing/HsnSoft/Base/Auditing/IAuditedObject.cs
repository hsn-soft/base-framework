namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was made and last changed, and by whom.</summary>
public interface IAuditedObject : ICreationAuditedObject, IModificationAuditedObject;
