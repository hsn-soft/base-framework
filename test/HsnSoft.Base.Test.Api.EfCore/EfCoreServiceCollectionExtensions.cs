using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Services;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Test.Api.Domain.Repositories;
using HsnSoft.Base.Test.Api.EfCore.Context;
using HsnSoft.Base.Test.Api.EfCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Test.Api.EfCore;


public static class EfCoreServiceCollectionExtensions
{
    public static void AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AppEfCoreDbContext>(options =>
            {
                // options.UseInMemoryDatabase("TestDb");
                options.UseNpgsql(configuration.GetConnectionString(EfCoreDbProperties.ConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                    sqlOptions.MigrationsAssembly(typeof(AppEfCoreDbContext).Assembly.GetName().Name);
                    sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(6), null);
                    sqlOptions.CommandTimeout(30000);
                    sqlOptions.MaxBatchSize(100);
                });

                options.EnableSensitiveDataLogging(true);
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
        services.AddScoped<IUnitOfWork, UnitOfWork<AppEfCoreDbContext>>();

        // Repositories
        services.AddScoped(typeof(IEfCoreGenericRepository<,>), typeof(EfCoreGenericRepository<,>));
        services.AddScoped<IEfCoreUserRepository, EfCoreUserRepository>();
    }
}