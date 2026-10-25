using Abc.OrderService.Domain.OrderDomain.Repositories;
using Abc.OrderService.EntityFrameworkCore.Context;
using Abc.OrderService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.OrderService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the order service needs of PostgreSQL: the <c>OrderDbContext</c> with the provider (<c>AddBasePostgres</c>, connection string <c>ConnectionStrings:Postgres</c>, and its readiness check) and the repositories.
    /// </summary>
    public static IServiceCollection AddOrderPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBasePostgres<OrderDbContext>(configuration);

        services.AddScoped<IOrderRepository, EfCoreOrderRepository>();
        return services;
    }
}
