using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Hosting;

public static partial class StartupSteps
{
    /// <summary>
    /// Runs a startup step (create a schema, create indexes, seed data) and retries while the dependency it talks to is
    /// still starting, so the services of a system can be started in any order.
    /// </summary>
    /// <param name="services">The application services (used for logging only).</param>
    /// <param name="name">Names the step in the log.</param>
    /// <param name="step">The work; throwing means "the dependency is not ready yet".</param>
    /// <param name="maxAttempts">After this many failed attempts the last exception is thrown.</param>
    /// <param name="retryDelay">Pause between attempts; defaults to two seconds.</param>
    public static async Task RunStartupStepAsync(
        this IServiceProvider services,
        string name,
        Func<CancellationToken, Task> step,
        int maxAttempts = 15,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        ILogger? logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(StartupSteps).FullName!);
        TimeSpan delay = retryDelay ?? TimeSpan.FromSeconds(2);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await step(CancellationToken.None).ConfigureAwait(false);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                if (logger is not null)
                {
                    LogWaiting(logger, name, attempt, maxAttempts, exception.Message);
                }

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Step} is waiting for its dependency (attempt {Attempt}/{Max}): {Reason}")]
    private static partial void LogWaiting(ILogger logger, string step, int attempt, int max, string reason);
}
