using Abc.Shared.Hosting.Gateways.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Abc.Shared.Hosting.Gateways;

/// <summary>Turns the route registry and the service registry into what YARP needs: a route with a path transform, and a cluster with the address of its service.</summary>
public static class ReverseProxyConfigBuilder
{
    private const string ClusterSuffix = "-cluster";
    private const string ServiceSuffix = "-service";

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(ReverseProxyRouteRegistryOptions routeOptions, ServiceEndpointRegistryOptions serviceOptions)
    {
        ArgumentNullException.ThrowIfNull(routeOptions);
        ArgumentNullException.ThrowIfNull(serviceOptions);

        List<RouteConfig> routes = [];
        Dictionary<string, ClusterConfig> clusters = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string routeId, ReverseProxyRouteItem route) in routeOptions.Routes)
        {
            routes.Add(new RouteConfig
            {
                RouteId = routeId,
                ClusterId = route.ClusterId,
                Match = new RouteMatch { Path = route.MatchPath },
                Transforms = [new Dictionary<string, string> { ["PathPattern"] = route.DownstreamPathPattern }],
            });

            if (clusters.ContainsKey(route.ClusterId))
            {
                continue;
            }

            string serviceKey = ServiceKeyOf(route.ClusterId);
            if (!serviceOptions.Services.TryGetValue(serviceKey, out ServiceEndpointItem? service) || string.IsNullOrWhiteSpace(service.Origin))
            {
                throw new InvalidOperationException($"ServiceEndpoints:Services:{serviceKey}:Origin is required by the route '{routeId}'.");
            }

            clusters[route.ClusterId] = new ClusterConfig
            {
                ClusterId = route.ClusterId,
                Destinations = new Dictionary<string, DestinationConfig> { ["d1"] = new() { Address = service.Origin.Trim().TrimEnd('/') + "/" } },
                HttpRequest = new ForwarderRequestConfig { ActivityTimeout = ActivityTimeoutOf(routeOptions, route.ClusterId) },
            };
        }

        return (routes, [.. clusters.Values]);
    }

    // identity-cluster -> identity-service
    internal static string ServiceKeyOf(string clusterId)
        => clusterId.EndsWith(ClusterSuffix, StringComparison.OrdinalIgnoreCase)
            ? clusterId[..^ClusterSuffix.Length] + ServiceSuffix
            : throw new InvalidOperationException($"The cluster '{clusterId}' must be named <service>{ClusterSuffix} (identity{ClusterSuffix} is the service identity{ServiceSuffix}).");

    private static TimeSpan? ActivityTimeoutOf(ReverseProxyRouteRegistryOptions routeOptions, string clusterId)
    {
        string? text = routeOptions.Routes.Values
            .FirstOrDefault(route => route.ClusterId.Equals(clusterId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(route.ActivityTimeout))
            ?.ActivityTimeout;
        return text is null ? null : TimeSpan.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }
}
