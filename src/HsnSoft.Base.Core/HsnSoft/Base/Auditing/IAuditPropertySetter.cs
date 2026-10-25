namespace HsnSoft.Base.Auditing;

/// <summary>Fills the audit fields of an entity: the time from the clock and the user from <see cref="Security.ICurrentUser"/>. What does not implement the audit contracts is left as it is.</summary>
public interface IAuditPropertySetter
{
    /// <summary>For an entity that is being added: <see cref="ICreationAudited"/> and, as it is also the first change, <see cref="IModificationAudited"/>.</summary>
    void SetCreation(object entity);

    /// <summary>For an entity that was changed: <see cref="IModificationAudited"/>.</summary>
    void SetModification(object entity);

    /// <summary>For an entity that is being deleted: <see cref="ISoftDelete"/> (once) and <see cref="IModificationAudited"/>.</summary>
    void SetDeletion(object entity);
}
