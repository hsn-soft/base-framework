namespace HsnSoft.Base.Auditing;

/// <summary>Shows when a record was made and by whom.</summary>
public interface ICreationAuditedObject : IHasCreationTime, IMayHaveCreator;
