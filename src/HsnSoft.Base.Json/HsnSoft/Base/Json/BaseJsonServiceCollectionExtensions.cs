using HsnSoft.Base.Json.Newtonsoft;
using HsnSoft.Base.Json.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Json;

public static class BaseJsonServiceCollectionExtensions
{
    public static IServiceCollection AddBaseJsonServiceCollection(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor
            .Transient<IConfigureOptions<BaseSystemTextJsonSerializerOptions>, BaseSystemTextJsonSerializerOptionsSetup>());

        services.Configure<BaseJsonOptions>(options =>
        {
            options.Providers.Add<BaseNewtonsoftJsonSerializerProvider>();
            if (options.UseHybridSerializer)
            {
                options.Providers.Add<BaseSystemTextJsonSerializerProvider>();
            }
        });

        services.Configure<BaseNewtonsoftJsonSerializerOptions>(options =>
        {
            options.Converters.Add<BaseJsonIsoDateTimeConverter>();
        });

        return services;
    }
}