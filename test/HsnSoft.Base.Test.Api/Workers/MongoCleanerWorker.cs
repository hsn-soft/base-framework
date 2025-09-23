using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;

namespace HsnSoft.Base.Test.Api.Workers;

public class MongoCleanerWorker(IServiceScopeFactory scopeFactory, ILogger<MongoCleanerWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<MongoCleanerWorker> _logger = logger ??  throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // var retryPolicy = Policy
        //     .Handle<Exception>() // SqlException or DbUpdateException
        //     .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt),
        //         (exception, timespan, retryCount, ctx) =>
        //         {
        //             _logger.LogWarning(exception, "Cleaner retry {RetryCount}", retryCount);
        //         });

        while (!stoppingToken.IsCancellationRequested)
        {
            // await retryPolicy.ExecuteAsync(async () =>
            // {

            using var scope = _scopeFactory.CreateScope();
            var mongoUserRepo = scope.ServiceProvider.GetRequiredService<IMongoUserRepository>();

            var mongoUsers = await mongoUserRepo.GetListAsync(new ListQueryOptions<User> { Filter = u => u.Email.StartsWith("hasan") }, stoppingToken);
            if (mongoUsers.Count != 0)
            {
                // SOFT DELETE
                foreach (var user in mongoUsers)
                {
                    await mongoUserRepo.DeleteAsync(user, stoppingToken); // ISoftDelete
                }
            }
            else
            {
                _logger.LogInformation("No users to delete");
            }

            // });

            await Task.Delay(3000, stoppingToken);
        }
    }
}