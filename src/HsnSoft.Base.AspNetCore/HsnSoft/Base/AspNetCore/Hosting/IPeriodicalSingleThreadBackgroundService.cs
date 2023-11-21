using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Hosting;

public interface IPeriodicalSingleThreadBackgroundService
{
    protected bool WaitContinuousThread { get; set; }
    protected Task OperationAsync(CancellationToken cancellationToken);

    protected void TriggerOperationWithoutDelay();
}