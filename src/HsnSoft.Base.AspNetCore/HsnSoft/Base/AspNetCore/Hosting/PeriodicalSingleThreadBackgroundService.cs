using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.Hosting;

public abstract class PeriodicalSingleThreadBackgroundService<TService> : BackgroundService, IPeriodicalSingleThreadBackgroundService
    where TService : IPeriodicalSingleThreadBackgroundService
{
    private ILogger Log { get; set; }

    public TimeSpan PeriodRange { get; set; }

    public bool WaitContinuousThread { get; set; }

    private bool IsProcessing { get; set; }

    protected PeriodicalSingleThreadBackgroundService(TimeSpan periodRange, bool waitContinuousThread = false, ILogger logger = null)
    {
        Log = logger ?? LoggerFactory.Create(x => x.AddConsole()).CreateLogger(nameof(TService));
        PeriodRange = periodRange;
        WaitContinuousThread = waitContinuousThread;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.LogInformation($"{nameof(TService)} Execute - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");

        await BackgroundProcessing(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.LogInformation($"{typeof(TService).Name} Stopping - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
        while (IsProcessing && WaitContinuousThread)
        {
            // Wait is uncompleted tasks
            Task.Delay(500).GetAwaiter().GetResult();
        }

        Log.LogInformation($"{typeof(TService).Name} Stopped - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
        // Send cancellation token
        return base.StopAsync(cancellationToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IsProcessing = true;
            try
            {
                Log.LogInformation($"{typeof(TService).Name} Processing Begin - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
                await OperationAsync(stoppingToken);
                Log.LogInformation($"{typeof(TService).Name} Processing End - {DateTime.UtcNow:yyyyMMdd hh:mm:ss}");
            }
            catch (Exception ex)
            {
                Log.LogInformation($"{typeof(TService).Name} Operation Cancelled | {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }

            await Task.Delay(PeriodRange, stoppingToken);
        }
    }

    public abstract Task OperationAsync(CancellationToken cancellationToken);
}