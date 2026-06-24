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
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
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