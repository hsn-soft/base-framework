using Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson.Serialization;

namespace Edx.InvoiceService.MongoDB.Configurations;

/// <summary>How a <see cref="ReceivedFile"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class ReceivedFileClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(ReceivedFile)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<ReceivedFile>(map =>
        {
            map.MapProperty(file => file.FileId);
            map.MapProperty(file => file.Kind);
            map.MapProperty(file => file.Channel);
            map.MapProperty(file => file.FileName);
            map.MapProperty(file => file.SizeBytes);
            map.MapProperty(file => file.ContentType);
            map.MapProperty(file => file.Container);
            map.MapProperty(file => file.ObjectName);
            map.MapProperty(file => file.FolderPath);
            map.MapProperty(file => file.EntryCount);
            map.MapProperty(file => file.ReceivedAt);
            map.MapProperty(file => file.TenantId);
        });
    }
}
