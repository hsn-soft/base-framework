using System;

namespace HsnSoft.Base.Domain.Entities;

public class EntityDuplicateException : BaseException
{
    public EntityDuplicateException()
    {
    }

    public EntityDuplicateException(Type entityType)
        : this(entityType,  null)
    {
    }

    public EntityDuplicateException(Type entityType, Exception innerException)
        : base( $"There is one more entity given parameters. Entity type: {entityType.FullName}", innerException)
    {
        EntityType = entityType;
    }

    public EntityDuplicateException(string message)
        : base(message)
    {
    }

    public EntityDuplicateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public Type EntityType { get; set; }
}