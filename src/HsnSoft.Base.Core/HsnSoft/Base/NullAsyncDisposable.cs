using System;
using System.Threading.Tasks;

namespace HsnSoft.Base;

public sealed class NullAsyncDisposable : IAsyncDisposable
{
    private NullAsyncDisposable()
    {
    }

    public static NullAsyncDisposable Instance { get; } = new NullAsyncDisposable();

    public ValueTask DisposeAsync()
    {
        return default;
    }
}