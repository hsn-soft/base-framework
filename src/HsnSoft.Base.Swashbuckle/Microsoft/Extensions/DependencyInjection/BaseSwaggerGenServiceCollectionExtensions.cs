using System;
using System.Collections.Generic;
using HsnSoft.Base.Content;
using JetBrains.Annotations;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public static class BaseSwaggerGenServiceCollectionExtensions
{
    public static IServiceCollection AddBaseSwaggerGen(
        this IServiceCollection services,
        Action<SwaggerGenOptions> setupAction = null)
    {
        return services.AddSwaggerGen(options =>
        {
            var remoteStreamContentSchemaFactory = () => new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };

            options.MapType<RemoteStreamContent>(remoteStreamContentSchemaFactory);
            options.MapType<IRemoteStreamContent>(remoteStreamContentSchemaFactory);

            setupAction?.Invoke(options);
        });
    }

    public static IServiceCollection AddBaseSwaggerGenWithOAuth(
        this IServiceCollection services,
        [NotNull] string authority,
        [NotNull] Dictionary<string, string> scopes,
        Action<SwaggerGenOptions> setupAction = null)
    {
        return services
            .AddBaseSwaggerGen()
            .AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("oauth2",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri($"{authority.EnsureEndsWith('/')}connect/authorize"), Scopes = scopes, TokenUrl = new Uri($"{authority.EnsureEndsWith('/')}connect/token")
                            }
                        }
                    });

                options.AddSecurityRequirement(doc =>
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(
                                referenceId: "oauth2",
                                hostDocument: doc
                            ),
                            []
                        }
                    });

                setupAction?.Invoke(options);
            });
    }

    public static IServiceCollection AddBaseSwaggerGenWithBearer(
        this IServiceCollection services,
        [NotNull] string tokenDescription,
        Action<SwaggerGenOptions> setupAction = null)
    {
        return services
            .AddBaseSwaggerGen()
            .AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = tokenDescription,
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(doc =>
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(
                                referenceId: "Bearer",
                                hostDocument: doc
                            ),
                            []
                        }
                    });

                setupAction?.Invoke(options);
            });
    }
}