using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
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
        _instanceId = Guid.CreateVersion7();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("{Loader} | {OperationStatus} | {OperationTime}", nameof(LoaderHostedService), "STARTED", $"{DateTime.UtcNow:yyyyMMdd hh:mm:ss}");

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: "{Loader} | Started",
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
                _logger.LogDebug("{Loader} | {OperationStatus} | {OperationTime}", nameof(LoaderHostedService), "COMPLETED", $"{DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
                break;
            }
            catch (OperationCanceledException) { }

            _logger.LogError("{Loader} | {OperationStatus} | {OperationTime}", nameof(LoaderHostedService), "FAILED", $"{DateTime.UtcNow:yyyyMMdd hh:mm:ss}");

            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "{Loader} | Failed",
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
        _logger.LogDebug("{Loader} | {OperationStatus} | {OperationTime}", nameof(LoaderHostedService), "STOPPED", $"{DateTime.UtcNow:yyyyMMdd hh:mm:ss}");

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: "{Loader} | Stopped",
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