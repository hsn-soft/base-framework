using System.Net;
using Edx.Shared.Hosting.Gateways.Options;
using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edx.Shared.Hosting.Gateways;

/// <summary>The shell of every Edx gateway: the shared hosting, the Swagger of the services behind it, and the reverse proxy built from the route registry.</summary>
public static class GatewayRegister
{
    private const string CorsPolicyName = "edx-gateway";

    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ReverseProxyRoutes</c>, <c>ServiceEndpoints</c>, <c>SwaggerGateway</c>, <c>RequestLogging</c> (<c>Role</c> = <c>Gateway</c>) and, when users are authenticated, <c>Jwt</c>.</param>
    /// <param name="title">The title of the gateway's Swagger document.</param>
    /// <param name="authenticateUsers">
    /// Whether the gateway reads the bearer token, so its own logs know the user (the gateway). It does not reject anyone: the services decide who may
    /// call what, as every endpoint of a service needs a token unless it says otherwise.
    /// </param>
    public static IServiceCollection AddEdxGateway(this IServiceCollection services, IConfiguration configuration, string title, bool authenticateUsers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ReverseProxyRouteRegistryOptions routes = configuration.GetSection("ReverseProxyRoutes").Get<ReverseProxyRouteRegistryOptions>() ?? new();
        ServiceEndpointRegistryOptions endpoints = configuration.GetSection("ServiceEndpoints").Get<ServiceEndpointRegistryOptions>() ?? new();
        (IReadOnlyList<RouteConfig> proxyRoutes, IReadOnlyList<ClusterConfig> proxyClusters) = ReverseProxyConfigBuilder.Build(routes, endpoints);

        services
            .AddEdxHosting(configuration, title, swaggerName: "gateway", authenticateUsers: authenticateUsers)
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
        services.AddReverseProxy().LoadFromMemory(proxyRoutes, proxyClusters);

        // a back office in a browser calls the gateway from its own address: those origins, and nobody else's, may
        string[] origins = configuration.GetSection(GatewayPolicyOptions.SectionName).Get<GatewayPolicyOptions>()?.Cors.AllowedOrigins ?? [];
        if (origins.Length > 0)
        {
            services.AddCors(cors => cors.AddPolicy(CorsPolicyName, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Correlation-Id", "Content-Disposition")));
        }

        return services;
    }

    /// <summary>The limits of the public network: the biggest body, the time a client has to send its headers, how many headers.</summary>
    public static IWebHostBuilder ConfigureEdxGatewayKestrel(this IWebHostBuilder host)
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

    public static WebApplication UseEdxGateway(this WebApplication app, bool authenticateUsers)
    {
        ArgumentNullException.ThrowIfNull(app);

        // before everything that asks who the caller is: a preflight of a browser carries no token
        if (app.Configuration.GetSection(GatewayPolicyOptions.SectionName).Get<GatewayPolicyOptions>()?.Cors.AllowedOrigins is { Length: > 0 })
        {
            app.UseCors(CorsPolicyName);
        }

        app.UseEdxHosting(authenticateUsers);
        app.MapReverseProxy();
        return app;
    }
}
