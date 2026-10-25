using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Sorting;

/// <summary>How a list is sorted: the clauses, first the most important. Made by <see cref="SortFields{TEntity}.Parse"/> from what a client asked for.</summary>
public sealed class SortSpec<TEntity>(IReadOnlyList<SortClause<TEntity>> clauses)
{
    public IReadOnlyList<SortClause<TEntity>> Clauses { get; } = clauses;

    public void Accept(ISortVisitor<TEntity> visitor)
    {
        foreach (SortClause<TEntity> clause in Clauses)
        {
            clause.Accept(visitor);
        }
    }
}
