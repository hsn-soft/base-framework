using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMongoClient"/>, the <see cref="IMongoDatabase"/> named in the connection string and a readiness
    /// health check. Guids are stored as standard (UUID) binary values.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>, e.g. <c>mongodb://host:27017/mydatabase</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBaseMongo(this IServiceCollection services, IConfiguration configuration, string connectionName = "Mongo")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required.");
        var url = new MongoUrl(connectionString);
        string databaseName = url.DatabaseName
                           ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} must contain a database name.");

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(MongoClientSettings.FromUrl(url)));
        services.TryAddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
