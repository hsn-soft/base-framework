using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.EfCore.Context;

namespace HsnSoft.Base.Test.Api.Workers;

public class EfCoreWriterWorker(IServiceScopeFactory scopeFactory, ILogger<EfCoreWriterWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory??  throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<EfCoreWriterWorker> _logger = logger?? throw new ArgumentNullException(nameof(logger));
    private int _counter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // var retryPolicy = Policy
        //     .Handle<Exception>() // SqlException or DbUpdateException
        //     .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt),
        //         (exception, timespan, retryCount, ctx) =>
        //         {
        //             _logger.LogWarning(exception, "Writer retry {RetryCount}", retryCount);
        //         });

        while (!stoppingToken.IsCancellationRequested && _counter < 100)
        {
            // await retryPolicy.ExecuteAsync(async () =>
            // {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppEfCoreDbContext>();

            for (int i = 0; i < 100; i++)
            {
                var user = new User { FirstName = $"Hasan{i}", LastName = $"Tester{i}" };
                user.SetEmail($"{user.FirstName.ToLower()}_{Guid.CreateVersion7().ToString("N").ToLower()}@test.com");
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Inserted {User}", user.Email);
            }

            // });

            await Task.Delay(1000, stoppingToken);
            _counter++;
        }
    }
}