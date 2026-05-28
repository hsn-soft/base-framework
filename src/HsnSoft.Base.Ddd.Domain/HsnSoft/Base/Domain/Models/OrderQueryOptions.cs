using System;
using System.Linq;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Models;

public class OrderQueryOptions<T> : FilterQueryOptions<T>
{
    [CanBeNull] public string OrderByDynamic { get; init; }
    [CanBeNull] public Func<IQueryable<T>, IOrderedQueryable<T>> OrderByEntity { get; init; }
}