using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.ContentService.EntityFrameworkCore.Repositories;
using Hhs.Shared.Helper.EventInbox;
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

namespace Hhs.ContentService.EntityFrameworkCore;

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

        services.AddDbContext<ContentServiceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(ContentServiceDbContext).Assembly.GetName().Name);
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

        // unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork<ContentServiceDbContext>>();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));

        // Setting Domain Repositories
        services.AddScoped<ICustomerVpSettingRepository, EfCoreCustomerVpSettingRepository>();
        services.AddScoped<IContentVideoGenerationLimitRepository, EfCoreContentVideoGenerationLimitRepository>();
        // services.AddScoped<IResponseStatisticRepository, EfCoreResponseStatisticRepository>();

        // Content Domain Repositories
        services.AddScoped<ICustomerContentRepository, EfCoreCustomerContentRepository>();
        services.AddScoped<ICustomerContentVisitRepository, EfCoreCustomerContentVisitRepository>();
        services.AddScoped<IAnalysisContentRepository, EfCoreAnalysisContentRepository>();

        // Infra Domain Repositories
        services.AddScoped<IEventInboxMessageRepository, EfCoreEventInboxMessageRepository>();

        return services;
    }
}