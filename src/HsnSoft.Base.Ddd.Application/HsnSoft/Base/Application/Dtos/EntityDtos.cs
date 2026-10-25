using System;

namespace HsnSoft.Base.Application.Dtos;

public interface IEntityDto<TKey>
{
    TKey Id { get; set; }
}

/// <summary>The base of what an entity is shown as: its id.</summary>
public abstract class EntityDto<TKey> : IEntityDto<TKey>
{
    public TKey Id { get; set; } = default!;
}

/// <summary>An entity that is shown with when and by whom it was made.</summary>
public abstract class CreationAuditedEntityDto<TKey> : EntityDto<TKey>
{
    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }
}

/// <summary>An entity that is shown with its creation and its last change.</summary>
public abstract class AuditedEntityDto<TKey> : CreationAuditedEntityDto<TKey>
{
    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}

/// <summary>An entity that is shown with its deletion too (for the code that looks at the deleted ones; the lists do not show them).</summary>
public abstract class FullAuditedEntityDto<TKey> : AuditedEntityDto<TKey>
{
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}
