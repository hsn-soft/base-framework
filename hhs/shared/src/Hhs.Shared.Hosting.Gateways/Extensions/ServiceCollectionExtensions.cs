using Hhs.Shared.Hosting.Extensions;
using HsnSoft.Base.AspNetCore.Logging;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Tracing;
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

            services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
            services.AddSingleton<IRequestResponseLogger, RequestLogger>();

            return services;
        }
    }
}