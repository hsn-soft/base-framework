using System;
using System.Collections.Generic;
using System.Threading;

namespace HsnSoft.Base.Data;

/// <summary>The default <see cref="IDataFilter"/>: every filter is on until a scope turns it off.</summary>
public sealed class DataFilter : IDataFilter
{
    private readonly AsyncLocal<Dictionary<Type, bool>?> _states = new();

    public bool IsEnabled<TFilter>()
        where TFilter : class => _states.Value?.GetValueOrDefault(typeof(TFilter), true) ?? true;

    public IDisposable Enable<TFilter>()
        where TFilter : class => Set(typeof(TFilter), true);

    public IDisposable Disable<TFilter>()
        where TFilter : class => Set(typeof(TFilter), false);

    private Restore Set(Type filter, bool enabled)
    {
        Dictionary<Type, bool>? parent = _states.Value;

        // a new dictionary each time: the scopes of other flows of the code keep theirs
        _states.Value = new Dictionary<Type, bool>(parent ?? []) { [filter] = enabled };
        return new Restore(() => _states.Value = parent);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
