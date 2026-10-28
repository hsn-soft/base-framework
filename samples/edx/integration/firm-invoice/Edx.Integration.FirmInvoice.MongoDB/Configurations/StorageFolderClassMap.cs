using Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.Integration.FirmInvoice.MongoDB.Configurations;

/// <summary>How a <see cref="StorageFolder"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class StorageFolderClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(StorageFolder)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<StorageFolder>(map =>
        {
            map.MapProperty(folder => folder.ParentId);
            map.MapProperty(folder => folder.Name);
            map.MapProperty(folder => folder.Path);
            map.MapProperty(folder => folder.Level).SetSerializer(new EnumSerializer<FolderLevel>(BsonType.String));
            map.MapProperty(folder => folder.TenantId);
        });
    }
}
