using Hhs.FeedRService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.FeedRService.EntityFrameworkCore;

public sealed class EfCoreSeederService : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EfCoreSeederService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogInformation("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");

        var dbContext = scope.ServiceProvider.GetRequiredService<FeedRServiceDbContext>();
        try
        {
            if (dbContext.Database.CanConnectAsync(cancellationToken).GetAwaiter().GetResult())
            {
                if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogInformation("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogInformation("{WorkerName} | EVERYTHING IS UP TO DATE", nameof(EfCoreSeederService));
                }
            }
            else
            {
                // first creation
                await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogInformation("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(EfCoreSeederService));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(EfCoreSeederService), "FAIL", e.Message);
        }
    }
}