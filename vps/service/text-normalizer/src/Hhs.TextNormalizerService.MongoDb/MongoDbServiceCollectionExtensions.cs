using Hhs.TextNormalizerService.Domain;
using Hhs.TextNormalizerService.Domain.InfraDomain.Repositories;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
using Hhs.TextNormalizerService.MongoDb.Configurations;
using Hhs.TextNormalizerService.MongoDb.Context;
using Hhs.TextNormalizerService.MongoDb.Repositories;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Hhs.TextNormalizerService.MongoDb;

public static class MongoDbServiceCollectionExtensions
{
    public static IServiceCollection AddServiceMongoDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBaseTimingServiceCollection();
        services.Configure<BaseClockOptions>(o => o.Kind = DateTimeKind.Utc);
        services.AddTransient<ICurrentUser, CurrentUser>(); // scope to transient because mongo-context singleton
        services.AddTransient<IAuditPropertySetter, AuditPropertySetter>();
        services.AddBaseDataServiceCollection();

        MongoConfigure();
        RegisterClassMaps();

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new TextNormalizerServiceDbContext(sp, configuration);
        });

        // Must be Scoped => Cannot consume any scoped service and CurrentUser object creation on constructor
        services.AddScoped(typeof(IMongoGenericRepository<,>), typeof(MongoGenericRepository<,>));

        // Setting Domain Repositories
        services.AddScoped<ICustomerVpSettingRepository, MongoCustomerVpSettingRepository>();

        // Normalize Domain Repositories
        services.AddScoped<ICustomerContentNormalizedRequestRepository, MongoCustomerContentNormalizedRequestRepository>();
        services.AddScoped<IAnalysisContentNormalizedRequestRepository, MongoAnalysisContentNormalizedRequestRepository>();

        // Infra Domain Repositories
        services.AddScoped<IEventInboxMessageRepository, MongoEventInboxMessageRepository>();

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
            if (type is null) throw new ArgumentNullException();
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

        // SettingDomain configuration
        CustomerVpSettingClassMap.Register();

        // NormalizeDomain configuration
        CustomerContentNormalizedRequestClassMap.Register();
        AnalysisContentNormalizedRequestClassMap.Register();

        // InfraDomain configuration
        EventInboxMessageClassMap.Register();
    }
}