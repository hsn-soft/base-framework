using Hhs.Gateway.Commercial.Options;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Hhs.Gateway.Commercial.Services;

public sealed class ReverseProxyConfigBuilder(
    IOptions<ReverseProxyRouteRegistryOptions> routeOptions,
    IOptions<ServiceEndpointRegistryOptions> serviceOptions)
{
    private readonly ReverseProxyRouteRegistryOptions _routeOptions = routeOptions.Value;
    private readonly ServiceEndpointRegistryOptions _serviceOptions = serviceOptions.Value;

    public (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build()
    {
        var routes = new List<RouteConfig>();
        var clusters = new Dictionary<string, ClusterConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var routeKvp in _routeOptions.Routes)
        {
            var routeId = routeKvp.Key;
            var route = routeKvp.Value;

            routes.Add(new RouteConfig
            {
                RouteId = routeId,
                ClusterId = route.ClusterId,
                Match = new RouteMatch
                {
                    Path = route.MatchPath
                },
                Transforms = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["PathPattern"] = route.DownstreamPathPattern
                    }
                }
            });

            if (!clusters.ContainsKey(route.ClusterId))
            {
                var serviceKey = MapClusterIdToServiceKey(route.ClusterId);

                if (!_serviceOptions.Services.TryGetValue(serviceKey, out var service))
                    throw new InvalidOperationException($"Service endpoint tanımı bulunamadı: {serviceKey}");

                clusters[route.ClusterId] = new ClusterConfig
                {
                    ClusterId = route.ClusterId,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["d1"] = new()
                        {
                            Address = NormalizeAddress(service.Origin)
                        }
                    },
                    HttpRequest = new ForwarderRequestConfig
                    {
                        ActivityTimeout = ResolveActivityTimeout(route.ClusterId)
                    }
                };
            }
        }

        return (routes, clusters.Values.ToList());
    }

    private TimeSpan? ResolveActivityTimeout(string clusterId)
    {
        var timeoutText = _routeOptions.Routes.Values
            .FirstOrDefault(x => x.ClusterId.Equals(clusterId, StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrWhiteSpace(x.ActivityTimeout))
            ?.ActivityTimeout;

        if (string.IsNullOrWhiteSpace(timeoutText))
            return null;

        return TimeSpan.Parse(timeoutText);
    }

    private static string NormalizeAddress(string origin)
    {
        origin = origin.Trim();
        if (!origin.EndsWith('/'))
            origin += "/";

        return origin;
    }

    private static string MapClusterIdToServiceKey(string clusterId)
    {
        return clusterId switch
        {
            "administration-cluster" => "administration-service",
            "identity-cluster" => "identity-service",
            "content-cluster" => "content-service",
            "text-normalizer-cluster" => "text-normalizer-service",
            "video-generator-cluster" => "video-generator-service",
            "event-manager-cluster" => "event-manager-service",
            "feedr-admanager-cluster" => "feedr-admanager-service",
            "feedr-weather-cluster" => "feedr-weather-service",
            _ => throw new InvalidOperationException($"ClusterId için service map bulunamadı: {clusterId}")
        };
    }
}