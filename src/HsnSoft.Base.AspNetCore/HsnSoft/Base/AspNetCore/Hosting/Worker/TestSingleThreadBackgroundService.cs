using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.Hosting.Worker;

public sealed class TestSingleThreadBackgroundService : BaseSingleThreadBackgroundService<TestSingleThreadBackgroundService>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TestSingleThreadBackgroundService(IBaseLogger logger,
        IServiceScopeFactory serviceScopeFactory
    ) : base(logger, waitPeriodSeconds: 30, waitContinuousThread: true)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public override async Task OperationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);

        using var scope = _serviceScopeFactory.CreateScope();
        // var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
        // await sampleAppService.SampleOperation();
    }
}