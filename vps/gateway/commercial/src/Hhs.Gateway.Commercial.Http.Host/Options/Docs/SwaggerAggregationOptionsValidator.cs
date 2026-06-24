using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class SwaggerAggregationOptionsValidator : IValidateOptions<SwaggerAggregationOptions>
{
    public ValidateOptionsResult Validate([CanBeNull] string name, SwaggerAggregationOptions options)
    {
        var errors = new List<string>();

        if (options.CacheDurationSeconds <= 0)
            errors.Add("SwaggerAggregation:CacheDurationSeconds 0'dan büyük olmalıdır.");

        if (options.HttpTimeoutSeconds <= 0)
            errors.Add("SwaggerAggregation:HttpTimeoutSeconds 0'dan büyük olmalıdır.");

        if (options.RetryCount < 0)
            errors.Add("SwaggerAggregation:RetryCount 0 veya daha büyük olmalıdır.");

        if (options.RetryDelayMilliseconds < 0)
            errors.Add("SwaggerAggregation:RetryDelayMilliseconds 0 veya daha büyük olmalıdır.");

        if (options.Services is null || options.Services.Count == 0)
            errors.Add("SwaggerAggregation:Services en az 1 eleman içermelidir.");

        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gatewayJsonPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in options.Services)
        {
            if (string.IsNullOrWhiteSpace(service.Key))
                errors.Add("SwaggerAggregation:Services[*]:Key boş olamaz.");

            if (string.IsNullOrWhiteSpace(service.DisplayName))
                errors.Add($"Swagger service '{service.Key}' için DisplayName boş olamaz.");

            if (string.IsNullOrWhiteSpace(service.DownstreamPathPrefix))
                errors.Add($"Swagger service '{service.Key}' için DownstreamPathPrefix boş olamaz.");

            if (string.IsNullOrWhiteSpace(service.GatewayPathPrefix))
                errors.Add($"Swagger service '{service.Key}' için GatewayPathPrefix boş olamaz.");

            if (!service.DownstreamPathPrefix.StartsWith('/'))
                errors.Add($"Swagger service '{service.Key}' için DownstreamPathPrefix '/' ile başlamalıdır.");

            if (!service.GatewayPathPrefix.StartsWith('/'))
                errors.Add($"Swagger service '{service.Key}' için GatewayPathPrefix '/' ile başlamalıdır.");

            if (!string.IsNullOrWhiteSpace(service.ServiceHealthPath)
                && !service.ServiceHealthPath.StartsWith('/'))
            {
                errors.Add($"Swagger service '{service.Key}' için ServiceHealthPath '/' ile başlamalıdır.");
            }

            if (!string.IsNullOrWhiteSpace(service.GatewayHealthPath)
                && !service.GatewayHealthPath.StartsWith('/'))
            {
                errors.Add($"Swagger service '{service.Key}' için GatewayHealthPath '/' ile başlamalıdır.");
            }

            if (!keySet.Add(service.Key))
                errors.Add($"SwaggerAggregation:Services içinde duplicate Key bulundu: '{service.Key}'.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}