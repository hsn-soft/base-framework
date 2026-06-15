using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Hhs.VideoGeneratorService.Mongo;

public static class BsonRegisterTools
{
    public static void MongoConfigure()
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
}