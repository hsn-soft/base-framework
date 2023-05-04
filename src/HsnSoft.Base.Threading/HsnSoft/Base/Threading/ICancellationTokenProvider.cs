using System;
using System.Threading;

namespace HsnSoft.Base.Threading;

public interface ICancellationTokenProvider
{
    CancellationToken Token { get; }

    IDisposable Use(CancellationToken cancellationToken);
}