using System;
using System.Linq.Expressions;

namespace HsnSoft.Base.Reflection;

/// <summary>
/// Puts a filter together from the parts of a request that were given: <c>new FilterBuilder&lt;Product&gt;().And(name is null ? null : p =&gt; p.NormalizedName.Contains(name)).And(...).Build()</c>. A part that is
/// <see langword="null"/> (not given) is left out, so a filter with no part matches everything.
/// </summary>
public sealed class FilterBuilder<T>
{
    private Expression<Func<T, bool>>? _filter;

    public FilterBuilder<T> And(Expression<Func<T, bool>>? predicate)
    {
        if (predicate is null)
        {
            return this;
        }

        _filter = _filter is null ? predicate : Combine(_filter, predicate);
        return this;
    }

    /// <summary>The filter; the one that matches everything when no part was given.</summary>
    public Expression<Func<T, bool>> Build() => _filter ?? (_ => true);

    private static Expression<Func<T, bool>> Combine(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), left.Parameters[0].Name);
        Expression leftBody = new Replacer(left.Parameters[0], parameter).Visit(left.Body);
        Expression rightBody = new Replacer(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class Replacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
