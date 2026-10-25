using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.EntityFrameworkCore.Context;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.OrderService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the order service needs of PostgreSQL: the <c>OrderDbContext</c> on the database that <c>Database:Provider</c> names (<c>postgres</c> with <c>ConnectionStrings:Postgres</c>, or <c>sqlserver</c> with <c>ConnectionStrings:SqlServer</c>; the code of the service does not change), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddOrderPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<OrderDbContext>(configuration, providers => providers.UsePostgres().UseSqlServer());

        services.AddEfCoreRepository<OrderDbContext, Order, Guid>();
        return services;
    }
}
