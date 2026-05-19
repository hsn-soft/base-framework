using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.DashboardDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.ContentService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Services;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    public static IServiceCollection AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBaseTimingServiceCollection();
        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();
        services.AddBaseDataServiceCollection();

        services.AddDbContext<ContentServiceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(ContentServiceDbContext).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
                });
                // options.EnableSensitiveDataLogging();
                // options.UseLoggerFactory(LoggerFactory.Create(builder =>
                // {
                //     builder.AddConsole();
                //     builder.SetMinimumLevel(LogLevel.Information);
                // }));
                options.EnableSensitiveDataLogging(false);
            }
            , contextLifetime: ServiceLifetime.Scoped // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
            , optionsLifetime: ServiceLifetime.Singleton
        );

        // unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork<ContentServiceDbContext>>();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));
        services.AddScoped<IClientRepository, EfCoreClientRepository>();
        services.AddScoped<IClientVideoGenerationHistoryRepository, EfCoreClientVideoGenerationHistoryRepository>();
        services.AddScoped<IAppContentRepository, EfCoreAppContentRepository>();
        services.AddScoped<IAppContentVisitRepository, EfCoreAppContentVisitRepository>();
        services.AddScoped<IAnalysisContentRepository, EfCoreAnalysisContentRepository>();
        services.AddScoped<IResponseStatisticRepository, EfCoreResponseStatisticRepository>();

        return services;
    }
}