using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.EntityFrameworkCore;

public sealed class EfCoreSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");


        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthServiceDbContext>();
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
                const string systemTenantName = "system";

                var systemTenant = await dbContext.AuthTenants
                    .FirstOrDefaultAsync(x => x.NormalizedName == systemTenantName.ToUpperInvariant(), cancellationToken: cancellationToken);

                if (systemTenant is null)
                {
                    systemTenant = new AuthTenant
                    {
                        Name = systemTenantName,
                        NormalizedName = systemTenantName.ToUpperInvariant(),
                        IsSystemTenant = true
                    };

                    dbContext.AuthTenants.Add(systemTenant);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                if (!await dbContext.AuthRoles.AnyAsync(x => x.TenantId == systemTenant.Id && x.NormalizedName == "REGISTERED", cancellationToken: cancellationToken))
                {
                    dbContext.AuthRoles.Add(new AuthRole
                    {
                        TenantId = systemTenant.Id,
                        Name = "registered",
                        NormalizedName = "REGISTERED"
                    });
                }

                if (!await dbContext.AuthPasswordPolicies.AnyAsync(x => x.TenantId == systemTenant.Id, cancellationToken: cancellationToken))
                {
                    dbContext.AuthPasswordPolicies.Add(new AuthPasswordPolicy
                    {
                        TenantId = systemTenant.Id,
                        MinLength = 8,
                        RequireDigit = true,
                        RequireLowercase = true,
                        RequireUppercase = true,
                        RequireNonAlphanumeric = false,
                        MaxFailedLoginCount = 5,
                        LockoutMinutes = 15
                    });
                }

                await dbContext.SaveChangesAsync(cancellationToken);














                // var managerRole = new AuthRole
                // {
                //     Name = "Manager",
                //     NormalizedName = "MANAGER"
                // };
                //
                // dbContext.AuthRoles.Add(managerRole);
                //
                // dbContext.AuthRoleClaims.AddRange(
                //     new AuthRoleClaim
                //     {
                //         Role = managerRole,
                //         ClaimType = "permission",
                //         ClaimValue = "invoice.create"
                //     },
                //     new AuthRoleClaim
                //     {
                //         Role = managerRole,
                //         ClaimType = "permission",
                //         ClaimValue = "invoice.update"
                //     },
                //     new AuthRoleClaim
                //     {
                //         Role = managerRole,
                //         ClaimType = "permission",
                //         ClaimValue = "invoice.delete"
                //     },
                //     new AuthRoleClaim
                //     {
                //         Role = managerRole,
                //         ClaimType = "permission",
                //         ClaimValue = "invoice.price.view"
                //     }
                // );
                //
                // await dbContext.SaveChangesAsync();

                // if (!dbContext.Roles.Any())
                // {
                //     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
                //
                //     foreach (var seedRole in SeedRoles.Roles)
                //     {
                //         var draftAppRole = new AppRole(
                //             id: seedRole.RoleId,
                //             name: seedRole.Name,
                //             isDefault: seedRole.IsDefault,
                //             isStatic: seedRole.IsStatic,
                //             isPublic: seedRole.IsPublic,
                //             tenantId: seedRole.TenantId,
                //             tenantDomain: seedRole.TenantDomain
                //         );
                //
                //         var result = await roleManager.CreateAsync(draftAppRole);
                //         if (!result.Succeeded) throw new AppRoleIdentityException(localizer, result.Errors);
                //
                //         await roleManager.AddClaimAsync(draftAppRole, new Claim("role_name",
                //             StringHelper.FirstCharCapitalize(seedRole.Name, new[] { "-", "_" })));
                //
                //         logger.LogDebug("{WorkerName} | SEED ROLE -> {RoleName} added", nameof(EfCoreSeederService), seedRole.Name);
                //     }
                // }

                // if (!dbContext.Users.Any())
                // {
                //     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                //
                //     foreach (var seedUser in SeedUsers.Users)
                //     {
                //         var draftAppUser = new AppUser(
                //             tenantId: seedUser.TenantId,
                //             tenantDomain: seedUser.TenantDomain,
                //             id: seedUser.UserId,
                //             userName: seedUser.Username,
                //             email: seedUser.Email,
                //             phone: seedUser.Phone,
                //             name: seedUser.GivenName,
                //             surname: seedUser.FamilyName,
                //             defaultLanguage: string.IsNullOrWhiteSpace(seedUser.Lang) ? "en" : seedUser.Lang,
                //             avatarSuffixUrl: string.IsNullOrWhiteSpace(seedUser.AvatarUrl) ? "/images/no-image.webp" : seedUser.AvatarUrl
                //         );
                //
                //         var result = string.IsNullOrWhiteSpace(seedUser.PlainPassword)
                //             ? await userManager.CreateAsync(draftAppUser)
                //             : await userManager.CreateAsync(draftAppUser, seedUser.PlainPassword);
                //
                //         if (!result.Succeeded) throw new AppUserIdentityException(localizer, result.Errors);
                //
                //         // add user roles
                //         if (seedUser.Roles is { Count: > 0 })
                //         {
                //             await userManager.AddToRolesAsync(draftAppUser, seedUser.Roles);
                //         }
                //
                //         logger.LogDebug("{WorkerName} | SEED USER -> {UserName} added", nameof(EfCoreSeederService), seedUser.Username);
                //     }
                // }
            }
            catch (Exception e)
            {
                logger.LogError("{WorkerName} | {OperationStatus}: {Error}", nameof(EfCoreSeederService), "SEED_ERROR", e.Message);
            }
        }
    }
}