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

    /// <summary>
    /// Adds a part that may stand in for the filter so far: the filter becomes <c>(what was added) OR part</c>. A part that is <see langword="null"/> is left out; with nothing added yet the part is the filter (a filter that matches everything
    /// does not "or" a part into everything). To look in several fields and then narrow by others, put the alternatives together first with <see cref="FilterBuilder.AnyOf{T}"/> and add the result with <see cref="And"/>.
    /// </summary>
    public FilterBuilder<T> Or(Expression<Func<T, bool>>? predicate)
    {
        if (predicate is null)
        {
            return this;
        }

        _filter = _filter is null ? predicate : Combine(_filter, predicate, orElse: true);
        return this;
    }

    /// <summary>Whether a part was given.</summary>
    internal bool HasParts => _filter is not null;

    /// <summary>The filter; the one that matches everything when no part was given.</summary>
    public Expression<Func<T, bool>> Build() => _filter ?? (_ => true);

    private static Expression<Func<T, bool>> Combine(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, bool orElse = false)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), left.Parameters[0].Name);
        Expression leftBody = new Replacer(left.Parameters[0], parameter).Visit(left.Body);
        Expression rightBody = new Replacer(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(orElse ? Expression.OrElse(leftBody, rightBody) : Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class Replacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}

/// <summary>What puts parts together for a <see cref="FilterBuilder{T}"/>.</summary>
public static class FilterBuilder
{
    /// <summary>
    /// One part that holds when any of the parts given does (<c>name contains x OR code contains x</c>); the parts that are <see langword="null"/> are left out, and <see langword="null"/> is the answer when none was given, so the result
    /// can go straight into <see cref="FilterBuilder{T}.And"/>.
    /// </summary>
    public static Expression<Func<T, bool>>? AnyOf<T>(params Expression<Func<T, bool>>?[] predicates)
    {
        ArgumentNullException.ThrowIfNull(predicates);

        FilterBuilder<T> builder = new();
        foreach (Expression<Func<T, bool>>? predicate in predicates)
        {
            builder.Or(predicate);
        }

        return builder.HasParts ? builder.Build() : null;
    }
}
