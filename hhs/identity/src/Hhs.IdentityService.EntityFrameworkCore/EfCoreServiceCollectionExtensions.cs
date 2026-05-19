using Hhs.IdentityService.EntityFrameworkCore.Context;
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

        services.AddDbContext<AuthServiceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(AuthServiceDbContext).Assembly.GetName().Name);
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
        services.AddScoped<IUnitOfWork, UnitOfWork<AuthServiceDbContext>>();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));
        // services.AddScoped<IFakeRepository, EfCoreFakeRepository>();

        return services;
    }
}