using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer;

public interface IPuppeteerBrowser : IDisposable
{
    public int ActivePagesCount { get; }
    public int PooledPagesCount { get; }
    public int MaxPagesCount { get; }
    public string InitializationResult { get; }

    [ItemCanBeNull]
    Task<IBrowser> GetBrowserSafelyAsync(CancellationToken cancellationToken = default);

    Task<IPage> GetPoolPageAsync(CancellationToken cancellationToken = default);
    void ReturnPoolPage(IPage page);
}