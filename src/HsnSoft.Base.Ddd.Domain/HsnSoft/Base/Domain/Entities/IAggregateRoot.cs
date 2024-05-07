namespace HsnSoft.Base.Domain.Entities;

public interface IAggregateRoot<out TKey> : IEntity<TKey>
{
}