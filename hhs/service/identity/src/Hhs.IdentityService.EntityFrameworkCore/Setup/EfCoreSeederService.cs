using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

public sealed class EfCoreSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");


        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        try
        {
            if (dbContext.Database.CanConnectAsync(cancellationToken).GetAwaiter().GetResult())
            {
                if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogDebug("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED (APP)", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogDebug("{WorkerName} | EVERYTHING IS UP TO DATE (APP)", nameof(EfCoreSeederService));
                }
            }
            else
            {
                // first creation
                await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogDebug("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(EfCoreSeederService));
            }

            isReadyDatabase = true;
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(EfCoreSeederService), "INIT_ERROR", e.Message);
        }

        if (isReadyDatabase)
        {
            try
            {
                using (dataFilter.Disable<IMultiTenant>())
                {
                    await AuthSeeder.SeedAsync(dbContext, logger, passwordHasher);

                    await SubscriptionSeeder.SeedAsync(dbContext, logger, passwordHasher);
                }
            }
            catch (Exception e)
            {
                logger.LogError("{WorkerName} | {OperationStatus}: {Error}", nameof(EfCoreSeederService), "SEED_ERROR", e.Message);
            }
        }
    }
}