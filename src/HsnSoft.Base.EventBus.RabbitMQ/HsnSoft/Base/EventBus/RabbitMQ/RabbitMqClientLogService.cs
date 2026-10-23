using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>Listens to the RabbitMQ client library while the host runs (see <see cref="RabbitMqClientLogBridge"/>).</summary>
internal sealed class RabbitMqClientLogService(ILoggerFactory loggerFactory) : IHostedService, IDisposable
{
    private RabbitMqClientLogBridge? _bridge;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _bridge = new RabbitMqClientLogBridge(loggerFactory.CreateLogger("HsnSoft.Base.EventBus.RabbitMQ.Client"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _bridge?.Dispose();
        _bridge = null;
    }
}
