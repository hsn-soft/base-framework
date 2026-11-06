using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Hsx.ContentService.Domain.AnalysisContentDomain.Entities;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Entities;
using Hsx.ContentService.EntityFrameworkCore.Context;
using Hsx.ContentService.EntityFrameworkCore.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.ContentService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the content service needs of PostgreSQL: the <c>ContentDbContext</c> on the provider and the connection string that <see cref="EfCoreContentDbProperties"/> names (<c>ConnectionStrings:EfCore-ContentService</c>), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddContentPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<ContentDbContext>(configuration, providers => providers.UsePostgres(), EfCoreContentDbProperties.Provider, EfCoreContentDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();
        services.AddEfCoreRepository<ContentDbContext, CustomerContent, Guid>();
        services.AddEfCoreRepository<ContentDbContext, AnalysisContent, Guid>();
        services.AddEfCoreRepository<ContentDbContext, AnalysisContentItem, Guid>();
        services.AddEfCoreRepository<ContentDbContext, CustomerContentVisit, Guid>();
        services.AddEfCoreRepository<ContentDbContext, CustomerContentVpSetting, Guid>();

        // the schema is migrated when the host starts, by the host that owns the database (SeedStartupConnections): the migrations of this project, in their order
        services.AddMigrationStartupStep<ContentDbContext>(EfCoreContentDbProperties.ConnectionStringName);
        services.AddDataSeeder<SampleContentSeeder>(EfCoreContentDbProperties.ConnectionStringName);   // demo contents of the demo customer (off with Seeding:Disabled)
        return services;
    }
}
