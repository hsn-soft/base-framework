using Hhs.Shared.Hosting.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Gateways.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGatewayHosting(IConfiguration configuration)
        {
            services.AddCommonAspNetCoreHosting(configuration);
            services.Configure<GatewayHostingSettings>(configuration.GetSection("HostingSettings"));

            services.AddRequestResponseLogger();

            return services;
        }
    }
}