using Hhs.IdentityService.Domain.AppRoleDomain.Repositories;
using Hhs.IdentityService.Domain.AppUserDomain.Repositories;
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

namespace Hhs.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    public static IServiceCollection AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration, bool showDetailLogs = false)
    {
        services.AddBaseTimingServiceCollection();
        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();
        services.AddBaseDataServiceCollection();

        AddAuthServerJwtDatabaseConfiguration(services, configuration);

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddTransient<IAppUserRepository, AppUserRepository>();
        services.AddTransient<IAppRoleRepository, AppRoleRepository>();

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
                options.EnableSensitiveDataLogging(false);

                if (!showDetailLogs) return;
                options.EnableSensitiveDataLogging();
                options.UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                }));
            }
            , contextLifetime: ServiceLifetime.Scoped // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
            , optionsLifetime: ServiceLifetime.Singleton
        );

        // unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork<IdentityServiceDbContext>>();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));
        // services.AddScoped<IFakeRepository, EfCoreFakeRepository>();

        return services;
    }

    public static IServiceCollection AddAuthServerJwtDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityAppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                sqlOptions.MigrationsAssembly(typeof(IdentityAppDbContext).Assembly.GetName().Name);
                sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), errorCodesToAdd: null);
                sqlOptions.CommandTimeout(30000);
                sqlOptions.MaxBatchSize(100);
            });
            // options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            options.EnableSensitiveDataLogging(false);
        });

        return services;
    }
}