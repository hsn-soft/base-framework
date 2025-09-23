using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Test.Api.Domain.Consts;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;
using HsnSoft.Base.Test.Api.EfCore.Context;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Api.Workers;

public class EfCoreCleanerWorker(IServiceScopeFactory scopeFactory, ILogger<EfCoreCleanerWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory??  throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<EfCoreCleanerWorker> _logger = logger?? throw new ArgumentNullException(nameof(logger));

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
            var efCoreUserRepo = scope.ServiceProvider.GetRequiredService<IEfCoreUserRepository>();

            var efCoreUsers = await efCoreUserRepo.GetListAsync(new ListQueryOptions<User> { Filter = u => u.Email.StartsWith("hasan") }, stoppingToken);
            if (efCoreUsers.Count != 0)
            {
                // SOFT DELETE
                foreach (var user in efCoreUsers)
                {
                    await efCoreUserRepo.DeleteAsync(user, stoppingToken); // ISoftDelete
                }
            }
            else
            {
                _logger.LogInformation("No users to delete");
            }

            // HARD DELETE
            var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
            var efCoredbContext = scope.ServiceProvider.GetRequiredService<AppEfCoreDbContext>();
            using (dataFilter.Disable<ISoftDelete>())
            {
                var tmpList = await efCoreUserRepo.GetListAsync(new ListQueryOptions<User> { Filter = u => u.IsDeleted }, stoppingToken);
                if (tmpList.Count != 0)
                {
                    try
                    {
                        foreach (var tmp in tmpList)
                        {
                            int res = await efCoredbContext.Database.ExecuteSqlRawAsync(
                                $"DELETE FROM \"{UserConsts.TableName}\" WHERE \"Id\" = {{0}}", [tmp.Id],
                                stoppingToken
                            );

                            _logger.LogInformation("Deleted {Count} user {email}", res, tmp.Email);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error deleting users");
                    }
                }
                else
                {
                    _logger.LogInformation("No users to delete");
                }
            }

            // });

            await Task.Delay(3000, stoppingToken);
        }
    }
}