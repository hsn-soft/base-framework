using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Hosting.Loader;

public class LoaderHostedService : IHostedService
{
    private readonly IFrameworkLogger _logger;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly Guid _instanceId;

    public LoaderHostedService(IServiceScopeFactory scopeFactory, IFrameworkLogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _instanceId = Guid.NewGuid();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LoaderHostedService | Started");

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: "LoaderHostedService | Started",
            reference: null,
            facility: "APPLICATION_LOADER_STARTED",
            correlationId: _instanceId.ToString(),
            exception: null
        ));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LoadConfiguration(cancellationToken);
                _logger.LogInformation($"LoaderHostedService | Successfully completed - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
                break;
            }
            catch (OperationCanceledException) { }

            _logger.LogError($"LoaderHostedService | Failed - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");

            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "LoaderHostedService | Failed",
                reference: null,
                facility: "APPLICATION_LOADER_FAILED",
                correlationId: _instanceId.ToString(),
                exception: null
            ));
            break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LoaderHostedService | Stopped");

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: "LoaderHostedService | Stopped",
            reference: null,
            facility: "APPLICATION_LOADER_STOPPED",
            correlationId: _instanceId.ToString(),
            exception: null
        ));

        return Task.CompletedTask;
    }

    private async Task LoadConfiguration(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<IBasicLoader>();
        await loader.LoadAsync(cancellationToken);
    }
}