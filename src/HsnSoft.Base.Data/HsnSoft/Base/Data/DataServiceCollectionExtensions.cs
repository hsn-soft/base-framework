using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IDataFilter"/>, the switch of the filters of soft delete and tenants that follows the flow of the code; what the host registered before is kept.</summary>
    public static IServiceCollection AddBaseDataFilter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataFilter, DataFilter>();
        return services;
    }
}
