using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.IdentityService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the identity service needs of PostgreSQL: the <c>IdentityDbContext</c> with the provider (<c>AddBasePostgres</c>, connection string <c>ConnectionStrings:Postgres</c>, and its readiness check) and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBasePostgres<IdentityDbContext>(configuration);

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        return services;
    }
}
