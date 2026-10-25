using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Repositories;
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
    /// Registers what the identity service needs of PostgreSQL: the <c>IdentityDbContext</c> on the database that <c>Database:Provider</c> names (<c>postgres</c> with <c>ConnectionStrings:Postgres</c>, or <c>sqlserver</c> with <c>ConnectionStrings:SqlServer</c>; the code of the service does not change), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityDbContext>(configuration, providers => providers.UsePostgres().UseSqlServer());

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        services.AddScoped<IRepository<User, Guid>>(provider => provider.GetRequiredService<IUserRepository>());
        return services;
    }
}
