using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.IntakeService.MongoDB.Configurations;

/// <summary>How a <see cref="StoredFile"/> and the entries of an archive are stored.</summary>
public static class StoredFileClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(StoredFile)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<ArchiveEntry>(map =>
        {
            map.MapProperty(entry => entry.Name);
            map.MapProperty(entry => entry.SizeBytes);
        });
        BsonClassMap.RegisterClassMap<StoredFile>(map =>
        {
            // the kind and the channel are kept as their names, so a person reading the collection understands them
            map.MapProperty(file => file.Kind).SetSerializer(new EnumSerializer<FileKind>(BsonType.String));
            map.MapProperty(file => file.Channel).SetSerializer(new EnumSerializer<IntakeChannel>(BsonType.String));
            map.MapProperty(file => file.FileName);
            map.MapProperty(file => file.ContentType);
            map.MapProperty(file => file.SizeBytes);
            map.MapProperty(file => file.FolderId);
            map.MapProperty(file => file.FolderPath);
            map.MapProperty(file => file.Container);
            map.MapProperty(file => file.ObjectName);
            map.MapProperty(file => file.Entries);
            map.MapProperty(file => file.TenantId);
        });
    }
}
