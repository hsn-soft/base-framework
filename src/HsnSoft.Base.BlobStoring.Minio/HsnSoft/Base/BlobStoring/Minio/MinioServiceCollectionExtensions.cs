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
            var options = provider.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build();
        });
        services.TryAddSingleton<IBlobStore, MinioBlobStore>();
        services.AddHealthChecks().AddCheck<MinioHealthCheck>("minio", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
