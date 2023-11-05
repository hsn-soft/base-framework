using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.Hosting;

public abstract class WebAppLoaderHostedService : IHostedService
{
    private ILogger Log { get; set; }

    public WebAppLoaderHostedService(ILogger logger)
    {
        Log = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.LogInformation("WebAppLoaderHostedService Service Start : {0:yyyyMMdd hh:mm:ss}", DateTime.Now);
        var isLoadConfigurationSuccessfully = false;
        const byte maxTryCount = 5;
        byte tryCount = 0;
        while (!cancellationToken.IsCancellationRequested && tryCount < maxTryCount)
        {
            tryCount++;

            Log.LogInformation($"WebAppLoaderHostedService - Configuration Loading... [{tryCount.ToString()}] {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
            isLoadConfigurationSuccessfully = await Process(cancellationToken);
            if (!isLoadConfigurationSuccessfully) continue;
            Log.LogInformation($"WebAppLoaderHostedService Successfully Loaded - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
            break;
        }

        if (!isLoadConfigurationSuccessfully) throw new Exception("Loading error");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.LogInformation("WebAppLoaderHostedService Service Stop : {0:yyyyMMdd hh:mm:ss}", DateTime.Now);
        await Task.CompletedTask;
    }

    protected abstract Task<bool> Process(CancellationToken cancellationToken);
}