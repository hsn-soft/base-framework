using Hhs.Gateway.Commercial.Options;
using Hhs.Gateway.Commercial.Options.Docs;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Services.Swagger;

public interface IServiceUrlResolver
{
    string GetGatewayPublicOrigin();
    string GetServiceOrigin(string serviceKey);

    string BuildServiceUrl(string serviceKey, [CanBeNull] string relativePath);
    string BuildGatewayUrl([CanBeNull] string relativePath);
}

public sealed class ServiceUrlResolver(
    IOptions<GatewayEndpointOptions> gatewayOptions,
    IOptions<ServiceEndpointRegistryOptions> serviceRegistryOptions) : IServiceUrlResolver
{
    private readonly GatewayEndpointOptions _gatewayOptions = gatewayOptions.Value;
    private readonly ServiceEndpointRegistryOptions _serviceRegistryOptions = serviceRegistryOptions.Value;

    public string GetGatewayPublicOrigin()
    {
        return NormalizeOrigin(_gatewayOptions.PublicOrigin);
    }

    public string GetServiceOrigin(string serviceKey)
    {
        if (!_serviceRegistryOptions.Services.TryGetValue(serviceKey, out var item))
        {
            throw new InvalidOperationException($"Service origin tanımı bulunamadı: {serviceKey}");
        }

        return NormalizeOrigin(item.Origin);
    }

    public string BuildServiceUrl(string serviceKey, [CanBeNull] string relativePath)
    {
        return Combine(GetServiceOrigin(serviceKey), relativePath);
    }

    public string BuildGatewayUrl([CanBeNull] string relativePath)
    {
        return Combine(GetGatewayPublicOrigin(), relativePath);
    }

    private static string NormalizeOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new InvalidOperationException("Origin boş olamaz.");

        return origin.TrimEnd('/');
    }

    private static string Combine(string origin, [CanBeNull] string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/")
            return origin + "/";

        if (!relativePath.StartsWith('/'))
            relativePath = "/" + relativePath;

        return origin + relativePath;
    }
}