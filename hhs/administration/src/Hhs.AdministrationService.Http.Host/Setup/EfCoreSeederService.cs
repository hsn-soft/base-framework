using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.Setup;

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
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");

        var dbContext = scope.ServiceProvider.GetRequiredService<AdministrationServiceDbContext>();
        try
        {
            if (dbContext.Database.CanConnectAsync(cancellationToken).GetAwaiter().GetResult())
            {
                if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogDebug("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogDebug("{WorkerName} | EVERYTHING IS UP TO DATE", nameof(EfCoreSeederService));
                }
            }
            else
            {
                // first creation
                await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogDebug("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(EfCoreSeederService));
            }

            if (!dbContext.PermissionGrants.Any())
            {
                foreach (string permissionName in StaticData.AllPermissions())
                {
                    dbContext.PermissionGrants.Add(new PermissionGrant(
                        id: Guid.NewGuid(),
                        name: permissionName,
                        providerName: "R",
                        providerKey: DefaultRoleNames.SystemAdmin)
                    );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#techsummus")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#dunya")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#kisadalga")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#tamindir")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#technotoday")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#sondakika")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#t24")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#cnbce")
                    // );
                    // dbContext.PermissionGrants.Add(new PermissionGrant(
                    //     id: Guid.NewGuid(),
                    //     name: permissionName,
                    //     providerName: "R",
                    //     providerKey: $"{NameConsts.Admin}#boxofficeturkiye")
                    // );
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (string permissionName in StaticData.DefaultUserPermissions())
                {
                    dbContext.PermissionGrants.Add(new PermissionGrant(
                        id: Guid.NewGuid(),
                        name: permissionName,
                        providerName: "R",
                        providerKey: DefaultRoleNames.SystemUser)
                    );
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogDebug("{WorkerName} | Seed PERMISSION GRANTS successfully completed", nameof(EfCoreSeederService));
            }
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(EfCoreSeederService), "FAIL", e.Message);
        }
    }
}