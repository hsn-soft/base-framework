using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Hhs.Gateway.Commercial.Services;

public sealed class CustomInMemoryConfigProvider : IProxyConfigProvider
{
    private readonly InMemoryProxyConfig _config;

    public CustomInMemoryConfigProvider(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        _config = new InMemoryProxyConfig(routes, clusters);
    }

    public IProxyConfig GetConfig() => _config;

    private sealed class InMemoryProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters) : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes { get; } = routes;
        public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;
        public IChangeToken ChangeToken { get; } = new CancellationChangeToken(new CancellationToken());
    }
}