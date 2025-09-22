using System;
using System.Linq.Expressions;

namespace HsnSoft.Base.Reflection;

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>() => f => true;

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}