using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer;

public interface IPuppeteerBrowser : IDisposable
{
    int ActivePagesCount { get; }
    int PooledPagesCount { get; }
    int MaxPagesCount { get; }
    string InitializationResult { get; }
    bool HasProxyServer { get; }
    string[] Args { get; }

    [ItemCanBeNull]
    Task<IBrowser> GetBrowserSafelyAsync(CancellationToken cancellationToken = default);

    Task<IPage> GetPoolPageAsync(CancellationToken cancellationToken = default);
    void ReturnPoolPage(IPage page);
}