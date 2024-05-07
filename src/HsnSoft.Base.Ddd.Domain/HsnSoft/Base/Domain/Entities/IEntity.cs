namespace HsnSoft.Base.Domain.Entities;

public interface IEntity
{
    object[] GetKeys();
}

public interface IEntity<out TKey> : IEntity
{
    TKey Id { get; }
}