using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

public static class SwaggerGatewayExtensions
{
    internal const string ServicesPath = "/swagger/services";

    /// <summary>
    /// Shows the documents of the services behind the gateway in the gateway's own Swagger UI (<c>UseBaseSwagger</c>, after <c>AddBaseSwagger*</c>): the
    /// gateway fetches each from the internal address of its service (<c>SwaggerGateway:Services</c>) and shows it with the gateway's address and paths, so
    /// "Try it out" goes through the gateway. The services only have to serve their own document (<c>UseBaseSwagger</c> there).
    /// </summary>
    public static IServiceCollection AddBaseSwaggerGateway(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SwaggerGatewayOptions>().Bind(configuration.GetSection(SwaggerGatewayOptions.SectionName)).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SwaggerGatewayOptions>, SwaggerGatewayOptionsValidator>());
        services.AddMemoryCache();
        services.AddHttpClient(SwaggerDocumentRewriter.HttpClientName);
        services.TryAddSingleton<SwaggerDocumentRewriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISwaggerExtension, SwaggerGatewayExtension>());
        return services;
    }
}

internal sealed class SwaggerGatewayExtension(IOptions<SwaggerGatewayOptions> options, SwaggerDocumentRewriter rewriter) : ISwaggerExtension
{
    private readonly SwaggerGatewayOptions _options = options.Value;

    public IEnumerable<(string Url, string Name)> UiEndpoints
        => _options.Services.Select(service => ($"{SwaggerGatewayExtensions.ServicesPath}/{service.Key}/swagger.json", service.Name));

    public void Use(IApplicationBuilder app)
        => app.Map(SwaggerGatewayExtensions.ServicesPath, branch => branch.Run(ServeAsync));

    // /swagger/services/{key}/swagger.json
    private async Task ServeAsync(HttpContext context)
    {
        string[] parts = context.Request.Path.Value!.Trim('/').Split('/');
        SwaggerServiceDefinition? service = parts is [var key, "swagger.json"] && HttpMethods.IsGet(context.Request.Method)
            ? _options.Services.FirstOrDefault(candidate => string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase))
            : null;
        if (service is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Map moved the services path to the path base: the gateway's own path base is what is left of it
        string pathBase = context.Request.PathBase.Value![..^SwaggerGatewayExtensions.ServicesPath.Length];
        string origin = string.IsNullOrWhiteSpace(_options.PublicOrigin)
            ? $"{context.Request.Scheme}://{context.Request.Host}{pathBase}"
            : _options.PublicOrigin.TrimEnd('/');
        try
        {
            string json = await rewriter.GetAsync(service, origin, context.RequestAborted).ConfigureAwait(false);
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(json, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is SwaggerServiceUnavailableException or JsonException)
        {
            // the UI shows this where the document should be: say which service and that it is not the gateway's fault
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { statusCode = 502, statusMessages = new[] { $"The Swagger document of '{service.Key}' is not available." } }), context.RequestAborted).ConfigureAwait(false);
        }
    }
}
