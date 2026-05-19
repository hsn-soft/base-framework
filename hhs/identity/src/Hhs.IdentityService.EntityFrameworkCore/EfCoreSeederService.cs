using System.Security.Claims;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Exceptions;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.IdentityService.EntityFrameworkCore.Setup;
using Hhs.Shared.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore;

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
        var localizerFactory = scope.ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
        var localizer = localizerFactory.CreateMultiple(new List<Type>
        {
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });

        bool isReadyDatabase = false;
        var appDbContext = scope.ServiceProvider.GetRequiredService<IdentityAppDbContext>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
        try
        {
            if (appDbContext.Database.CanConnectAsync(cancellationToken).GetAwaiter().GetResult())
            {
                if ((await appDbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await appDbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogDebug("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED (APP)", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogDebug("{WorkerName} | EVERYTHING IS UP TO DATE (APP)", nameof(EfCoreSeederService));
                }

                if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogDebug("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED (SERVICE)", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogDebug("{WorkerName} | EVERYTHING IS UP TO DATE (SERVICE)", nameof(EfCoreSeederService));
                }
            }
            else
            {
                // first creation
                await appDbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
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
                if (!appDbContext.Roles.Any())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

                    foreach (var seedRole in SeedRoles.Roles)
                    {
                        var draftAppRole = new AppRole(
                            id: seedRole.RoleId,
                            name: seedRole.Name,
                            isDefault: seedRole.IsDefault,
                            isStatic: seedRole.IsStatic,
                            isPublic: seedRole.IsPublic,
                            tenantId: seedRole.TenantId,
                            tenantDomain: seedRole.TenantDomain
                        );

                        var result = await roleManager.CreateAsync(draftAppRole);
                        if (!result.Succeeded) throw new AppRoleIdentityException(localizer, result.Errors);

                        await roleManager.AddClaimAsync(draftAppRole, new Claim("role_name",
                            StringHelper.FirstCharCapitalize(seedRole.Name, new[] { "-", "_" })));

                        logger.LogDebug("{WorkerName} | SEED ROLE -> {RoleName} added", nameof(EfCoreSeederService), seedRole.Name);
                    }
                }

                if (!appDbContext.Users.Any())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                    foreach (var seedUser in SeedUsers.Users)
                    {
                        var draftAppUser = new AppUser(
                            tenantId: seedUser.TenantId,
                            tenantDomain: seedUser.TenantDomain,
                            id: seedUser.UserId,
                            userName: seedUser.Username,
                            email: seedUser.Email,
                            phone: seedUser.Phone,
                            name: seedUser.GivenName,
                            surname: seedUser.FamilyName,
                            defaultLanguage: string.IsNullOrWhiteSpace(seedUser.Lang) ? "en" : seedUser.Lang,
                            avatarSuffixUrl: string.IsNullOrWhiteSpace(seedUser.AvatarUrl) ? "/images/no-image.webp" : seedUser.AvatarUrl
                        );

                        var result = string.IsNullOrWhiteSpace(seedUser.PlainPassword)
                            ? await userManager.CreateAsync(draftAppUser)
                            : await userManager.CreateAsync(draftAppUser, seedUser.PlainPassword);

                        if (!result.Succeeded) throw new AppUserIdentityException(localizer, result.Errors);

                        // add user roles
                        if (seedUser.Roles is { Count: > 0 })
                        {
                            await userManager.AddToRolesAsync(draftAppUser, seedUser.Roles);
                        }

                        logger.LogDebug("{WorkerName} | SEED USER -> {UserName} added", nameof(EfCoreSeederService), seedUser.Username);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("{WorkerName} | {OperationStatus}: {Error}", nameof(EfCoreSeederService), "SEED_ERROR", e.Message);
            }
        }
    }
}