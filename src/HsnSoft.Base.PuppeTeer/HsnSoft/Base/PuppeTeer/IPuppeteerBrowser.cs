using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer;

public interface IPuppeteerBrowser : IAsyncDisposable, IDisposable
{
    int ActivePagesCount { get; }
    int MaxPagesCount { get; }
    string InitializationResult { get; }
    bool HasProxyServer { get; }
    string[] Args { get; }
    bool IsStopping { get; }

    [ItemCanBeNull]
    Task<IBrowser> GetBrowserSafelyAsync(CancellationToken cancellationToken = default);

    Task<PuppeteerPageLease> AcquirePageAsync(Func<IPage, CancellationToken, Task>? configurePage = null, CancellationToken cancellationToken = default);

    Task RequestShutdownAsync();
    Task<bool> RequestShutdownAndDrainAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}