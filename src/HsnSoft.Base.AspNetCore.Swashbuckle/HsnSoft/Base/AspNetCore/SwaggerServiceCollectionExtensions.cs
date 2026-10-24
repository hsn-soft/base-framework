using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HsnSoft.Base.AspNetCore;

public static class SwaggerServiceCollectionExtensions
{
    private const string BearerScheme = "Bearer";
    private const string OAuthScheme = "oauth2";

    /// <summary>
    /// Adds the Swagger document of the api: every endpoint in it, the types named by their full name (two types with the same name in two
    /// namespaces do not collide). Serve it with <see cref="UseBaseSwagger"/>. <paramref name="name"/> is the name of the document
    /// (<c>/swagger/{name}/swagger.json</c>, <c>gateway</c> for a gateway), <paramref name="configure"/> adds anything else Swashbuckle can do.
    /// </summary>
    public static IServiceCollection AddBaseSwagger(
        this IServiceCollection services,
        string title,
        IConfiguration? configuration = null,
        string version = "v1",
        string name = "v1",
        Action<SwaggerGenOptions>? configure = null)
        => Add(services, title, configuration, version, name, null, configure);

    /// <summary>As <see cref="AddBaseSwagger"/>, with an <c>Authorize</c> button that sends a JWT as <c>Authorization: Bearer</c>.</summary>
    public static IServiceCollection AddBaseSwaggerWithBearer(
        this IServiceCollection services,
        string title,
        string tokenDescription,
        IConfiguration? configuration = null,
        string version = "v1",
        string name = "v1",
        Action<SwaggerGenOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenDescription);

        return Add(services, title, configuration, version, name, options =>
        {
            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Description = tokenDescription,
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = BearerScheme,
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference(BearerScheme, document), [] } });
        }, configure);
    }

    /// <summary>As <see cref="AddBaseSwagger"/>, with an <c>Authorize</c> button for the OAuth2 authorization code flow of <paramref name="authority"/> (<c>connect/authorize</c>, <c>connect/token</c>).</summary>
    public static IServiceCollection AddBaseSwaggerWithOAuth(
        this IServiceCollection services,
        string title,
        string authority,
        IDictionary<string, string> scopes,
        IConfiguration? configuration = null,
        string version = "v1",
        string name = "v1",
        Action<SwaggerGenOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentNullException.ThrowIfNull(scopes);

        string root = authority.TrimEnd('/') + "/";
        return Add(services, title, configuration, version, name, options =>
        {
            options.AddSecurityDefinition(OAuthScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{root}connect/authorize"),
                        TokenUrl = new Uri($"{root}connect/token"),
                        Scopes = new Dictionary<string, string>(scopes),
                    },
                },
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { { new OpenApiSecuritySchemeReference(OAuthScheme, document), [] } });
        }, configure);
    }

    /// <summary>
    /// Serves the document and the UI (<c>/swagger</c>) of <c>AddBaseSwagger*</c>, unless the environment is one that runs for real (see
    /// <see cref="SwaggerOptions"/>): there it does nothing, so the same code runs everywhere and the configuration decides.
    /// </summary>
    public static IApplicationBuilder UseBaseSwagger(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        SwaggerDocument document = app.ApplicationServices.GetService<SwaggerDocument>()
                                   ?? throw new InvalidOperationException("Call AddBaseSwagger (or AddBaseSwaggerWithBearer / AddBaseSwaggerWithOAuth) before UseBaseSwagger.");
        SwaggerOptions options = app.ApplicationServices.GetRequiredService<IOptions<SwaggerOptions>>().Value;
        if (!options.IsEnabled(app.ApplicationServices.GetRequiredService<IHostEnvironment>()))
        {
            return app;
        }

        ISwaggerExtension[] extensions = [.. app.ApplicationServices.GetServices<ISwaggerExtension>()];
        foreach (ISwaggerExtension extension in extensions)
        {
            extension.Use(app);
        }

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.RoutePrefix = options.RoutePrefix;
            ui.SwaggerEndpoint($"/swagger/{document.Name}/swagger.json", document.Title);
            foreach ((string url, string name) in extensions.SelectMany(extension => extension.UiEndpoints))
            {
                ui.SwaggerEndpoint(url, name);
            }
        });
        return app;
    }

    private static IServiceCollection Add(
        IServiceCollection services,
        string title,
        IConfiguration? configuration,
        string version,
        string name,
        Action<SwaggerGenOptions>? setup,
        Action<SwaggerGenOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        OptionsBuilder<SwaggerOptions> bound = services.AddOptions<SwaggerOptions>();
        if (configuration is not null)
        {
            bound.Bind(configuration.GetSection(SwaggerOptions.SectionName));
        }

        services.AddSingleton(new SwaggerDocument(name, title));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(name, new OpenApiInfo { Title = title, Version = version });
            options.DocInclusionPredicate((_, _) => true);
            options.CustomSchemaIds(type => type.FullName);
            setup?.Invoke(options);
            configure?.Invoke(options);
        });
        return services;
    }
}

/// <summary>The document <c>AddBaseSwagger*</c> made, which <c>UseBaseSwagger</c> shows.</summary>
internal sealed record SwaggerDocument(string Name, string Title);
