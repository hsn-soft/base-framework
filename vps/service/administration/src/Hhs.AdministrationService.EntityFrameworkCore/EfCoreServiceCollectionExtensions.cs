using Hhs.AdministrationService.Domain.InfraDomain.Repositories;
using Hhs.AdministrationService.Domain.MenuDomain.Repositories;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.AdministrationService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Hhs.AdministrationService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    private const int DbRetryCount = 10;
    private const int DbRetryDelaySeconds = 6;
    private const int DbCommandTimeoutMs = 30000;
    private const int DbMaxBatchSize = 100;

    public static IServiceCollection AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration, bool enabledSensitiveData = false)
    {
        services.AddBaseTimingServiceCollection();
        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();
        services.AddBaseDataServiceCollection();

        services.AddDbContext<AdministrationServiceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(AdministrationServiceDbContext).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(DbRetryCount, TimeSpan.FromSeconds(DbRetryDelaySeconds), null);
                    sqlOptions.CommandTimeout(DbCommandTimeoutMs);
                    sqlOptions.MaxBatchSize(DbMaxBatchSize);
                });
                options.EnableSensitiveDataLogging(enabledSensitiveData);
                options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddSerilog(Log.Logger); }));
            }
            , contextLifetime: ServiceLifetime.Scoped // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
            , optionsLifetime: ServiceLifetime.Singleton
        );

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));

        // Infra Domain Repositories
        services.AddScoped<IEventInboxMessageRepository, EfCoreEventInboxMessageRepository>();

        services.AddScoped<IAppMenuRepository, EfCoreAppMenuRepository>();
        services.AddScoped<IAppMenuPermissionRepository, EfCoreAppMenuPermissionRepository>();

        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();
        services.AddScoped<IPermissionRepository, EfCorePermissionRepository>();
        services.AddScoped<IAppRolePermissionRepository, EfCoreAppRolePermissionRepository>();
        services.AddScoped<IAppRolePermissionConstraintRepository, EfCoreAppRolePermissionConstraintRepository>();
        services.AddScoped<IPermissionDependencyRepository, EfCorePermissionDependencyRepository>();

        return services;
    }
}