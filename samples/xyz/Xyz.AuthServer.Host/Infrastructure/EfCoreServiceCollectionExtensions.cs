using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Xyz.AuthServer.Domain.UserDomain.Entities;
using Xyz.AuthServer.Domain.UserDomain.Repositories;
using Xyz.AuthServer.Domain.UserHistoryDomain.Entities;
using Xyz.AuthServer.Infrastructure.Context;
using Xyz.AuthServer.Infrastructure.Repositories;

namespace Xyz.AuthServer.Infrastructure;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the identity service needs of its database: the <c>IdentityDbContext</c> on the provider and the connection string that <see cref="EfCoreIdentityDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-AuthServer</c>), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityDbContext>(configuration, providers => providers.UsePostgres(), EfCoreIdentityDbProperties.Provider, EfCoreIdentityDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        services.AddScoped<IRepository<User, Guid>>(provider => provider.GetRequiredService<IUserRepository>());
        return services;
    }

    /// <summary>
    /// Registers the history context on the provider and the connection string that <see cref="EfCoreHistoryDbProperties"/> names (PostgreSQL, <c>ConnectionStrings:EfCore-AuthServerHistory</c>): a second database of the identity service,
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
