using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;

namespace HsnSoft.Base.BlobStoring.Minio;

public static class MinioServiceCollectionExtensions
{
    /// <summary>Registers the MinIO <see cref="IBlobStore"/> and a readiness health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>Minio</c> section (<see cref="MinioOptions"/>).</param>
    public static IServiceCollection AddMinioBlobStoring(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MinioOptions>(configuration.GetSection("Minio"));
        services.TryAddSingleton<IMinioClient>(provider =>
        {
            MinioOptions options = provider.GetRequiredService<IOptions<MinioOptions>>().Value;
            IMinioClient builder = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl);
            return (options.TimeoutSeconds is { } timeout ? builder.WithTimeout(timeout * 1000) : builder).Build();
        });
        services.TryAddSingleton<IBlobStore, MinioBlobStore>();
        services.AddHealthChecks().AddCheck<MinioHealthCheck>("minio", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
