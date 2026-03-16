using System;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerPageLease : IAsyncDisposable
{
    private readonly Func<IPage, Task> _releaseAsync;
    private int _disposed;

    public IPage Page { get; }

    internal PuppeteerPageLease(IPage page, Func<IPage, Task> releaseAsync)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        _releaseAsync = releaseAsync ?? throw new ArgumentNullException(nameof(releaseAsync));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await _releaseAsync(Page).ConfigureAwait(false);
    }
}