using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.Azure;

public static class AzureBlobServiceCollectionExtensions
{
    /// <summary>Registers the Azure <see cref="IBlobStore"/> and a readiness health check.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>AzureBlob</c> section (<see cref="AzureBlobOptions"/>): a connection string, or the address of the account.</param>
    public static IServiceCollection AddAzureBlobStoring(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));
        services.TryAddSingleton(provider => CreateClient(provider.GetRequiredService<IOptions<AzureBlobOptions>>().Value));
        services.TryAddSingleton<IBlobStore, AzureBlobStore>();
        services.AddHealthChecks().AddCheck<AzureBlobHealthCheck>("azure-blob", tags: [HealthCheckTags.Ready]);
        return services;
    }

    private static BlobServiceClient CreateClient(AzureBlobOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceUri))
        {
            return new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential());
        }

        throw new InvalidOperationException($"{AzureBlobOptions.SectionName}:ConnectionString or {AzureBlobOptions.SectionName}:ServiceUri is required.");
    }
}
