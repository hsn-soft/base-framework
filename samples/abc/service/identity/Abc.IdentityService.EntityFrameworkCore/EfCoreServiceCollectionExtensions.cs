using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.Domain.UserHistoryDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.IdentityService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the identity service needs of its database: the <c>IdentityDbContext</c> on the provider and the connection string that <see cref="EfCoreIdentityDbProperties"/> names (SQL Server, <c>ConnectionStrings:EfCore-IdentityService</c>), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityDbContext>(configuration, providers => providers.UseSqlServer(), EfCoreIdentityDbProperties.Provider, EfCoreIdentityDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        services.AddScoped<IRepository<User, Guid>>(provider => provider.GetRequiredService<IUserRepository>());
        return services;
    }

    /// <summary>
    /// Registers the history context on the provider and the connection string that <see cref="EfCoreHistoryDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-HistoryService</c>): a second database of the identity service,
    /// beside the one of <see cref="AddIdentityPersistence"/> and of another provider. Only the identity service has it; the auth server, which only reads the users, does not.
    /// </summary>
    public static IServiceCollection AddIdentityHistoryPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<HistoryDbContext>(configuration, providers => providers.UsePostgres(), EfCoreHistoryDbProperties.Provider, EfCoreHistoryDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();
        return services.AddEfCoreRepository<HistoryDbContext, UserHistory, Guid>();
    }
}
