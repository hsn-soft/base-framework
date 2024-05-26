using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Hosting.Worker;

public interface IBaseThreadBackgroundService
{
    protected Task OperationAsync(CancellationToken cancellationToken);
    protected void SkipOperationWaitPeriod();
}