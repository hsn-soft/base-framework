using System;

namespace HsnSoft.Base;

public sealed class NullDisposable : IDisposable
{
    private NullDisposable()
    {
    }

    public static NullDisposable Instance { get; } = new NullDisposable();

    public void Dispose()
    {
    }
}