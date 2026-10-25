using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Sorting;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>What a list is narrowed by and how it is sorted.</summary>
public class ListQueryOptions<TEntity>
{
    /// <summary>What an entity must match (build it with <c>FilterBuilder</c>); everything when <see langword="null"/>.</summary>
    public Expression<Func<TEntity, bool>>? Filter { get; init; }

    /// <summary>The sorting (<c>SortFields.Parse</c>); the order of the key when <see langword="null"/>.</summary>
    public SortSpec<TEntity>? Sort { get; init; }

    /// <summary>For a relational store: what to load with the entity (<c>query =&gt; query.Include(entity =&gt; entity.Parent)</c>). A document store ignores it.</summary>
    public Func<IQueryable<TEntity>, IQueryable<TEntity>>? Include { get; init; }

    /// <summary>At most this many; all when <see langword="null"/>.</summary>
    public int? MaxResultCount { get; init; }
}

/// <summary>A page of a list: the page counts from 1, and <see cref="ListQueryOptions{TEntity}.MaxResultCount"/> is how many a page has (10 when it is not given).</summary>
public class PagedQueryOptions<TEntity> : ListQueryOptions<TEntity>
{
    public const int DefaultPageSize = 10;

    public int PageNumber { get; init; } = 1;
}

/// <summary>A page and how many there are in all.</summary>
public sealed record PagedQueryResult<T>(IReadOnlyList<T> Items, long TotalCount);
