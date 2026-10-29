namespace HsnSoft.Base.Domain.Entities;

/// <summary>Something with an identity of its own.</summary>
public interface IEntity<out TKey>
{
    TKey Id { get; }
}
