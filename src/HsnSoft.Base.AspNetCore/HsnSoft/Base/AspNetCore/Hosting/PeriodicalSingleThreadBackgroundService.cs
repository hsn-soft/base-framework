using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.Hosting;

public abstract class PeriodicalSingleThreadBackgroundService<TService> : BackgroundService, IPeriodicalSingleThreadBackgroundService
    where TService : IPeriodicalSingleThreadBackgroundService
{
    public bool WaitContinuousThread { get; set; }

    private ILogger Log { get; set; }

    private int PeriodRangeSeconds { get; set; }

    private bool IsProcessing { get; set; }

    private bool TriggerIsActive { get; set; }

    protected PeriodicalSingleThreadBackgroundService(int periodRangeSeconds, bool waitContinuousThread = false, ILogger<TService> logger = null)
    {
        Log = logger ?? LoggerFactory.Create(x => x.AddConsole()).CreateLogger(typeof(TService).Name);
        PeriodRangeSeconds = periodRangeSeconds < 1 ? 60 : periodRangeSeconds;
        WaitContinuousThread = waitContinuousThread;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.LogInformation("{Name} -> Execute - {UtcNow}", typeof(TService).Name, DateTime.UtcNow);

        await BackgroundProcessing(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.LogInformation("{Name} -> Stopping - {UtcNow}", typeof(TService).Name, DateTime.UtcNow);
        while (IsProcessing && WaitContinuousThread)
        {
            // Wait is uncompleted tasks
            Task.Delay(500).GetAwaiter().GetResult();
        }

        Log.LogInformation("{Name} -> Stopped - {UtcNow}", typeof(TService).Name, DateTime.UtcNow);
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
                Log.LogInformation("{Name} -> Processing Begin - {UtcNow}", typeof(TService).Name, DateTime.UtcNow);
                await OperationAsync(stoppingToken);
                Log.LogInformation("{Name} -> Processing End - {UtcNow}", typeof(TService).Name, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Log.LogInformation("{Name} -> Operation Cancelled | {ExMessage}", typeof(TService).Name, ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }

            for (var i = 0; i < PeriodRangeSeconds; i++)
            {
                await Task.Delay(1000, stoppingToken);
                if (!TriggerIsActive) continue;
                TriggerIsActive = false;
                break;
            }
        }
    }

    public abstract Task OperationAsync(CancellationToken cancellationToken);

    public void TriggerOperationWithoutDelay()
    {
        TriggerIsActive = true;
    }
}