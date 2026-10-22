using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Xyz.Shared.Hosting;

public static partial class ServiceRegister
{
    /// <summary>A PostgreSQL <see cref="DbContext"/>. Connection string: <c>ConnectionStrings:Postgres</c>.</summary>
    public static IServiceCollection AddSharedPostgres<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        string connectionString = configuration.GetConnectionString("Postgres")
                                  ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        services.AddDbContext<TContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
        services.AddHealthChecks().AddCheck<DbContextHealthCheck<TContext>>("postgres", tags: ["ready"]);
        return services;
    }

    /// <summary>A MongoDB database (<see cref="IMongoDatabase"/>). The database name is part of <c>ConnectionStrings:Mongo</c>.</summary>
    public static IServiceCollection AddSharedMongo(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Mongo")
                                  ?? throw new InvalidOperationException("ConnectionStrings:Mongo is required.");
        var url = new MongoUrl(connectionString);
        string databaseName = url.DatabaseName ?? throw new InvalidOperationException("ConnectionStrings:Mongo must contain a database name.");

        // Guids are stored as standard (UUID v4/v7 compatible) binary values
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(MongoClientSettings.FromUrl(url)));
        services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);
        return services;
    }
}
