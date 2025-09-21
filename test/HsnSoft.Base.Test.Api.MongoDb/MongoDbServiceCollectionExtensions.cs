using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Test.Api.Domain;
using HsnSoft.Base.Test.Api.Domain.Repositories;
using HsnSoft.Base.Test.Api.MongoDb.Configurations;
using HsnSoft.Base.Test.Api.MongoDb.Context;
using HsnSoft.Base.Test.Api.MongoDb.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace HsnSoft.Base.Test.Api.MongoDb;

public static class MongoDbServiceCollectionExtensions
{
    public static IServiceCollection AddServiceMongoDbDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // services.AddBaseAuditingServiceCollection();
        // services.AddBaseDataServiceCollection();

        MongoConfigure();
        RegisterClassMaps();

        // DbContext
        services.AddSingleton(sp => new AppMongoDbContext(sp.GetRequiredService<IConfiguration>()));

        // Repositories
        services.AddScoped(typeof(IMongoGenericRepository<,>), typeof(MongoGenericRepository<,>));
        services.AddScoped<IMongoUserRepository, MongoUserRepository>();

        return services;
    }

    private static void MongoConfigure()
    {
        try
        {
            // MongoDB Guid support
            //BsonDefaults.GuidRepresentation = GuidRepresentation.Standard; // mongo v2.30
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch
        {
        }

        // MongoDB New version
        var objectSerializer = new ObjectSerializer(type =>
        {
            ArgumentNullException.ThrowIfNull(type);
            string scope = typeof(DomainAssemblyMarker).Namespace;
            return scope != null && type.FullName != null && (ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith(scope));
        });
        try
        {
            BsonSerializer.RegisterSerializer(objectSerializer);
        }
        catch
        {
        }

        // Conventions
        ConventionRegistry.Register("IgnoreConventions", new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            // new IgnoreIfDefaultConvention(true),
            new IgnoreIfNullConvention(false)
        }, t => true);

        ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
    }

    private static void RegisterClassMaps()
    {
        EntityClassMap.Register();
        UserClassMap.Register();
    }
}