using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Models;

public class FilterQueryOptions<T>
{
    [CanBeNull] public Expression<Func<T, bool>> Filter { get; init; }

    [CanBeNull] public Func<IQueryable<T>, IQueryable<T>> IncludeEntity { get; init; }
}