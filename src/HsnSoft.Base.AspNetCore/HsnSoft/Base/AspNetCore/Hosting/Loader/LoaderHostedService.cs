using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Hosting.Loader;

public class LoaderHostedService : IHostedService
{
    private readonly IBaseLogger _logger;

    private readonly IServiceScopeFactory _scopeFactory;

    public LoaderHostedService(IServiceScopeFactory scopeFactory, IBaseLogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LoaderHostedService | Started");

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
            break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LoaderHostedService | Stopped");
        return Task.CompletedTask;
    }

    private async Task LoadConfiguration(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<IBasicLoader>();
        await loader.LoadAsync(cancellationToken);
    }
}