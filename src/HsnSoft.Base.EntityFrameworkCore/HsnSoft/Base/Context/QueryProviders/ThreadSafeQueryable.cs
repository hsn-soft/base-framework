using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace HsnSoft.Base.Context.QueryProviders;

public class ThreadSafeQueryable : IOrderedQueryable
{
    protected readonly SemaphoreSlim SemaphoreSlim;
    public readonly IQueryable Set;

    public ThreadSafeQueryable(IQueryable set, SemaphoreSlim semaphoreSlim)
    {
        Set = set;
        SemaphoreSlim = semaphoreSlim;
    }


    public IEnumerator GetEnumerator()
    {
        return new ThreadSafeEnumerator(Set.GetEnumerator(), SemaphoreSlim);
    }

    public Type ElementType => Set.ElementType;

    public Expression Expression => Set.Expression;

    public IQueryProvider Provider => new ThreadSafeQueryProvider(Set.Provider, SemaphoreSlim);
}

public class ThreadSafeQueryable<T> : ThreadSafeQueryable, IOrderedQueryable<T>
{
    public ThreadSafeQueryable(IQueryable<T> set, SemaphoreSlim semaphoreSlim)
        : base(set, semaphoreSlim)
    {
    }

    public new IEnumerator<T> GetEnumerator()
    {
        return new ThreadSafeEnumerator<T>((Set as IQueryable<T>)!.GetEnumerator(), SemaphoreSlim);
    }
}

public sealed class ThreadSafeAsyncQueryable<T> : ThreadSafeQueryable<T>, IAsyncEnumerable<T>
{
    public ThreadSafeAsyncQueryable(IQueryable<T> set, SemaphoreSlim semaphoreSlim)
        : base(set, semaphoreSlim)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return new ThreadSafeAsyncEnumerator<T>(
            (Set as IAsyncEnumerable<T>)!.GetAsyncEnumerator(cancellationToken), SemaphoreSlim);
    }
}