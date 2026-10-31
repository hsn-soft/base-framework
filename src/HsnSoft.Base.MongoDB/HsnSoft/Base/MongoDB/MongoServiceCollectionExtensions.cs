using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMongoClient"/>, the <see cref="IMongoDatabase"/> named in the connection string, <see cref="MongoEntityConcepts"/> (audit fields and soft delete for the repositories; tenants are AddBaseMultiTenancyMongo of HsnSoft.Base.MultiTenancy.MongoDB) and a readiness
    /// health check. Guids are stored as standard (UUID) binary values.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>, e.g. <c>mongodb://host:27017/mydatabase</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    /// <param name="serviceKey">
    /// For a service that uses more than one MongoDB (another cluster, or another database): the key the client and the database are registered under (<c>GetRequiredKeyedService&lt;IMongoDatabase&gt;(key)</c>), and the one
    /// <c>AddMongoRepository</c> is given to use them. Without a key they are the one, unkeyed <see cref="IMongoDatabase"/> of the service.
    /// </param>
    public static IServiceCollection AddBaseMongo(this IServiceCollection services, IConfiguration configuration, string connectionName = "Mongo", string? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required.");
        var url = new MongoUrl(connectionString);
        string databaseName = url.DatabaseName
                           ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} must contain a database name.");

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        services.AddBaseEntityConcepts();
        services.TryAddSingleton<MongoEntityConcepts>();
        // who prepares this database (SeedStartupConnections) and how it is locked while the pods of the owner do it
        services.AddStartupDatabase(configuration, connectionName, provider => new MongoStartupLock(serviceKey is null ? provider.GetRequiredService<IMongoDatabase>() : provider.GetRequiredKeyedService<IMongoDatabase>(serviceKey)));
        if (serviceKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceKey);
            services.TryAddKeyedSingleton<IMongoClient>(serviceKey, (_, _) => new MongoClient(MongoClientSettings.FromUrl(url)));
            services.TryAddKeyedSingleton<IMongoDatabase>(serviceKey, (provider, key) => provider.GetRequiredKeyedService<IMongoClient>(key).GetDatabase(databaseName));
            services.AddHealthChecks().Add(new HealthCheckRegistration($"mongo-{serviceKey}", provider => new MongoHealthCheck(provider.GetRequiredKeyedService<IMongoDatabase>(serviceKey)), failureStatus: null, tags: [HealthCheckTags.Ready]));
            return services;
        }

        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(MongoClientSettings.FromUrl(url)));
        services.TryAddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
