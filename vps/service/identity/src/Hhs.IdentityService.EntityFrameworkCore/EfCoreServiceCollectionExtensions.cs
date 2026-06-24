using Hhs.IdentityService.Domain.AppRoleDomain.Repositories;
using Hhs.IdentityService.Domain.AppUserDomain.Repositories;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.InfraDomain.Repositories;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.IdentityService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Services;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Hhs.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    public static IServiceCollection AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration, bool enabledSensitiveData = false)
    {
        services.AddBaseTimingServiceCollection();
        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();
        services.AddBaseDataServiceCollection();

        services.AddDbContext<IdentityServiceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(IdentityServiceDbContext).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), errorCodesToAdd: null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
                });

                options.EnableSensitiveDataLogging(enabledSensitiveData);
                options.UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog(Log.Logger);
                }));
            }
            , contextLifetime: ServiceLifetime.Scoped // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
            , optionsLifetime: ServiceLifetime.Singleton
        );

        // unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork<IdentityServiceDbContext>>();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));

        // Infra Domain Repositories
        services.AddScoped<IEventInboxMessageRepository, EfCoreEventInboxMessageRepository>();

        services.AddScoped<ITenantRepository, EfCoreTenantRepository>();

        services.AddScoped<IAppRoleRepository, EfCoreAppRoleRepository>();
        services.AddScoped<IAppRoleClaimRepository, EfCoreAppRoleClaimRepository>();

        services.AddScoped<IAppUserRepository, EfCoreAppUserRepository>();
        services.AddScoped<IAppUserClaimRepository, EfCoreAppUserClaimRepository>();

        services.AddScoped<IAppUserRoleRepository, EfCoreAppUserRoleRepository>();

        services.AddScoped<IAuthEmailConfirmationTokenRepository, EfCoreAuthEmailConfirmationTokenRepository>();
        services.AddScoped<IAuthLoginAuditRepository, EfCoreAuthLoginAuditRepository>();
        services.AddScoped<IAuthPasswordPolicyRepository, EfCoreAuthPasswordPolicyRepository>();
        services.AddScoped<IAuthRefreshTokenRepository, EfCoreAuthRefreshTokenRepository>();
        services.AddScoped<IAuthTokenRevocationRepository, EfCoreAuthTokenRevocationRepository>();

        return services;
    }
}