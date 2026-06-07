using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.EntityFrameworkCore.Setup;

public sealed class EfCoreSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogInformation("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");

        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentServiceDbContext>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
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

            isReadyDatabase = true;
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(EfCoreSeederService), "FAIL", e.Message);
        }

        if (isReadyDatabase)
        {
            try
            {
                using (dataFilter.Disable<IMultiTenant>())
                {
                    await ClientSeeder.SeedAsync(dbContext, logger);
                }
            }
            catch (Exception e)
            {
                logger.LogError("{WorkerName} | {OperationStatus}: {Error}", nameof(EfCoreSeederService), "SEED_ERROR", e.Message);
            }
        }
    }
}