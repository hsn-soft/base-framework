using Hhs.Gateway.Commercial.Models;
using Hhs.Gateway.Commercial.Options.Docs;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Services.Swagger;

public sealed class GatewaySwaggerUiConfigurator(IOptions<SwaggerAggregationOptions> options)
{
    private readonly SwaggerAggregationOptions _options = options.Value;

    public IReadOnlyList<SwaggerUiEndpointItem> GetEndpoints()
    {
        var result = new List<SwaggerUiEndpointItem>
        {
            new()
            {
                Name = "Gateway - Host API",
                JsonUrl = "/swagger/gateway/swagger.json"
            }
        };

        foreach (var service in _options.Services)
        {
            result.Add(new SwaggerUiEndpointItem
            {
                Name = service.DisplayName,
                JsonUrl =$"/openapi-proxy/{service.Key}"
            });
        }

        return result;
    }
}