using System;
using System.Linq.Expressions;

namespace HsnSoft.Base.Domain.Sorting;

/// <summary>Receives the clauses of a <see cref="SortSpec{TEntity}"/> in order; a repository implements it for its own kind of query (a queryable, a MongoDB sort).</summary>
public interface ISortVisitor<TEntity>
{
    void Visit<TKey>(Expression<Func<TEntity, TKey>> key, bool descending);
}

/// <summary>One <c>field asc|desc</c> of a sorting, with the expression that sorts by it.</summary>
public abstract class SortClause<TEntity>
{
    public abstract void Accept(ISortVisitor<TEntity> visitor);
}

internal sealed class SortClause<TEntity, TKey>(Expression<Func<TEntity, TKey>> key, bool descending) : SortClause<TEntity>
{
    public override void Accept(ISortVisitor<TEntity> visitor) => visitor.Visit(key, descending);
}
