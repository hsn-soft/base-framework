using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Models;

public class ListQueryOptions<T> : OrderQueryOptions<T>
{
    public uint? ListLength { get; init; }
}

public class PaginationQueryOptions<T> : OrderQueryOptions<T>
{
    public uint PageNumber { get; init; } = 1;
    public uint PageSize { get; init; } = 5;
}

public class OrderQueryOptions<T>
{
    [CanBeNull] public Expression<Func<T, bool>> Filter { get; init; }
    [CanBeNull] public string OrderByDynamic { get; init; }
    [CanBeNull] public Func<IQueryable<T>, IOrderedQueryable<T>> OrderByEntity { get; init; }
}