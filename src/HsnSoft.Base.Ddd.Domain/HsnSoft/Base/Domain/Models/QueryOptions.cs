using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Models;

public class OrderQueryOptions<T>
{
    [CanBeNull] public Expression<Func<T, bool>> Filter { get; init; }
    [CanBeNull] public string OrderByDynamic { get; init; }
    [CanBeNull] public Func<IQueryable<T>, IOrderedQueryable<T>> OrderByEntity { get; init; }
}