using System.Net;
using HsnSoft.Base.AspNetCore;
using Hsx.Shared.Hosting.Gateways.Options;
using Hsx.Shared.Hosting.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace Hsx.Shared.Hosting.Gateways;

/// <summary>The shell of every Hsx gateway: the shared hosting, the Swagger of the services behind it, and the reverse proxy built from the route registry.</summary>
public static class GatewayRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ReverseProxyRoutes</c>, <c>ServiceEndpoints</c>, <c>SwaggerGateway</c>, <c>RequestLogging</c> (<c>Role</c> = <c>Gateway</c>) and, when users are authenticated, <c>Jwt</c>.</param>
    /// <param name="title">The title of the gateway's Swagger document.</param>
    public static IServiceCollection AddHsxGateway(this IServiceCollection services, IConfiguration configuration, string title)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ReverseProxyRouteRegistryOptions routes = configuration.GetSection("ReverseProxyRoutes").Get<ReverseProxyRouteRegistryOptions>() ?? new();
        ServiceEndpointRegistryOptions endpoints = configuration.GetSection("ServiceEndpoints").Get<ServiceEndpointRegistryOptions>() ?? new();
        (IReadOnlyList<RouteConfig> proxyRoutes, IReadOnlyList<ClusterConfig> proxyClusters) = ReverseProxyConfigBuilder.Build(routes, endpoints);

        services
            .AddHsxHttpHosting(configuration, title, swaggerName: "gateway")
            .AddBaseSwaggerGateway(configuration)   // the documents of the services, with the gateway's paths
            .Configure<GatewayPolicyOptions>(configuration.GetSection(GatewayPolicyOptions.SectionName));
        services.AddOptions<ForwardedHeadersOptions>().Configure<IOptions<GatewayPolicyOptions>>((forwarded, policy) =>
        {
            // the proxy in front of the gateway is trusted: the client address is the one it saw
            foreach (string proxy in policy.Value.TrustedProxyIps)
            {
                if (IPAddress.TryParse(proxy, out IPAddress? address))
                {
                    forwarded.KnownProxies.Add(address);
                }
            }
        });
        services.AddReverseProxy().LoadFromMemory((IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig>)proxyRoutes, (IReadOnlyList<Yarp.ReverseProxy.Configuration.ClusterConfig>)proxyClusters);
        return services;
    }

    /// <summary>The limits of the public network: the biggest body, the time a client has to send its headers, how many headers.</summary>
    public static IWebHostBuilder ConfigureHsxGatewayKestrel(this IWebHostBuilder host)
    {
        ArgumentNullException.ThrowIfNull(host);

        return host.ConfigureKestrel((context, options) =>
        {
            GatewayPolicyOptions policy = context.Configuration.GetSection(GatewayPolicyOptions.SectionName).Get<GatewayPolicyOptions>() ?? new();
            options.Limits.MaxRequestBodySize = policy.Kestrel.MaxRequestBodySizeBytes;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(policy.Kestrel.RequestHeadersTimeoutSeconds);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(policy.Kestrel.KeepAliveTimeoutSeconds);
            options.Limits.MaxRequestHeaderCount = policy.Kestrel.MaxRequestHeaderCount;
            options.Limits.MaxRequestHeadersTotalSize = policy.Kestrel.MaxRequestHeadersTotalSizeBytes;
        });
    }

    public static WebApplication UseHsxGateway(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // the first middleware of the hosting believes the proxies of Hosting:ForwardedHeaders and of GatewayPolicies:TrustedProxyIps
        app.UseHsxHttpHosting();
        app.MapReverseProxy();
        return app;
    }
}
