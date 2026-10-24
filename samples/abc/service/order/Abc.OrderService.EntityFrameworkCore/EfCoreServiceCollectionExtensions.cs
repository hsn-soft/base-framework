using Microsoft.Extensions.DependencyInjection;

namespace Abc.OrderService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the repositories of the order service (they are added here as the domain has them). The <c>OrderDbContext</c> itself is registered by the host with the
    /// database provider it chose (<c>AddBasePostgres&lt;OrderDbContext&gt;</c>).
    /// </summary>
    public static IServiceCollection AddOrderPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
