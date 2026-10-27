using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.Aws;

public static class AwsBlobServiceCollectionExtensions
{
    /// <summary>Registers the Amazon S3 <see cref="IBlobStore"/> and a readiness health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>AwsS3</c> section (<see cref="AwsBlobOptions"/>).</param>
    public static IServiceCollection AddAwsBlobStoring(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AwsBlobOptions>(configuration.GetSection(AwsBlobOptions.SectionName));
        services.TryAddSingleton<IAmazonS3>(provider => CreateClient(provider.GetRequiredService<IOptions<AwsBlobOptions>>().Value));
        services.TryAddSingleton<IBlobStore, AwsBlobStore>();
        services.AddHealthChecks().AddCheck<AwsBlobHealthCheck>("aws-s3", tags: [HealthCheckTags.Ready]);
        return services;
    }

    private static AmazonS3Client CreateClient(AwsBlobOptions options)
    {
        bool custom = !string.IsNullOrWhiteSpace(options.ServiceUrl);
        AmazonS3Config config = new()
        {
            ForcePathStyle = options.ForcePathStyle ?? custom,

            // what S3-compatible stores that do not know the newer checksums need; AWS accepts it
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (custom)
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
        }
        else if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return string.IsNullOrWhiteSpace(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
    }
}
