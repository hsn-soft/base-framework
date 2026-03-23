using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.Hosting.Worker;

public sealed class TestMultiThreadBackgroundService : BaseMultiThreadBackgroundService<TestMultiThreadBackgroundService>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TestMultiThreadBackgroundService(IFrameworkLogger logger,
        IServiceScopeFactory serviceScopeFactory
    ) : base(logger, multiThreadCount: 5, waitPeriodSeconds: 10, waitContinuousThread: true)
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