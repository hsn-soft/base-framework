using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.BlobStoring.FileSystem;

public static class FileSystemServiceCollectionExtensions
{
    /// <summary>Registers the local disk <see cref="IBlobStore"/> and a readiness health check (the root can be written).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>FileSystem</c> section (<see cref="FileSystemBlobOptions"/>): <c>RootPath</c> is required.</param>
    public static IServiceCollection AddFileSystemBlobStoring(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileSystemBlobOptions>(configuration.GetSection(FileSystemBlobOptions.SectionName));
        services.TryAddSingleton<FileSystemBlobStore>();
        services.TryAddSingleton<IBlobStore>(provider => provider.GetRequiredService<FileSystemBlobStore>());
        services.AddHealthChecks().AddCheck<FileSystemHealthCheck>("blob-filesystem", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
