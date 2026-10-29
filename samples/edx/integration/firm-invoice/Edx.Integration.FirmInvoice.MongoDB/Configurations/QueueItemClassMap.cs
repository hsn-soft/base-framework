using Edx.Integration.FirmInvoice.Domain.QueueDomain.Entities;
using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.Integration.FirmInvoice.MongoDB.Configurations;

/// <summary>How a <see cref="QueueItem"/> is stored: the enums as their names, so a person reading the collection understands them.</summary>
public static class QueueItemClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(QueueItem)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<QueueItem>(map =>
        {
            map.MapProperty(item => item.InvoiceId);
            map.MapProperty(item => item.InvoiceNumber);
            map.MapProperty(item => item.ReceivedBy);
            map.MapProperty(item => item.SourceType).SetSerializer(new EnumSerializer<InvoiceSourceType>(BsonType.String));
            map.MapProperty(item => item.ChannelType).SetSerializer(new EnumSerializer<InvoiceChannelType>(BsonType.String));
            map.MapProperty(item => item.FileHash);
            map.MapProperty(item => item.FileName);
            map.MapProperty(item => item.ContentType);
            map.MapProperty(item => item.SizeBytes);
            map.MapProperty(item => item.EntryName);
            map.MapProperty(item => item.FolderId);
            map.MapProperty(item => item.FolderPath);
            map.MapProperty(item => item.Container);
            map.MapProperty(item => item.ObjectName);
            map.MapProperty(item => item.Status).SetSerializer(new EnumSerializer<QueueStatus>(BsonType.String));
            map.MapProperty(item => item.StatusReason);
            map.MapProperty(item => item.StatusChangedAt);
            map.MapProperty(item => item.TenantId);
        });
    }
}
