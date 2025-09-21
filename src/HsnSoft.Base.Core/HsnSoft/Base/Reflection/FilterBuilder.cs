using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace HsnSoft.Base.Reflection;

public class FilterBuilder<T>
{
    private Expression<Func<T, bool>> _expression;
    public FilterBuilder() => _expression = PredicateBuilder.True<T>();

    public FilterBuilder<T> And([CanBeNull] Expression<Func<T, bool>> predicate)
    {
        if (predicate != null)
            _expression = _expression.And(predicate);
        return this;
    }

    public Expression<Func<T, bool>> Build() => _expression;
}