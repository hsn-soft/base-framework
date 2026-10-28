using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.InvoiceService.MongoDB.Configurations;

/// <summary>How an <see cref="Invoice"/> and its lines are stored: the domain has no attributes of the driver, the mapping is here. Amounts are decimals of the database (<c>Decimal128</c>), so no cent is lost; the enums are their names.</summary>
public static class InvoiceClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Invoice)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<InvoiceLine>(map =>
        {
            map.MapProperty(line => line.Description);
            map.MapProperty(line => line.Quantity).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(line => line.UnitPrice).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(line => line.VatRate).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.UnmapProperty(line => line.Total);
        });
        BsonClassMap.RegisterClassMap<Invoice>(map =>
        {
            map.MapProperty(invoice => invoice.InvoiceId);
            map.MapProperty(invoice => invoice.Number);
            map.MapProperty(invoice => invoice.NormalizedNumber);
            map.MapProperty(invoice => invoice.QueueId);
            map.MapProperty(invoice => invoice.SourceType).SetSerializer(new EnumSerializer<InvoiceSourceType>(BsonType.String));
            map.MapProperty(invoice => invoice.ChannelType).SetSerializer(new EnumSerializer<InvoiceChannelType>(BsonType.String));
            map.MapProperty(invoice => invoice.Status).SetSerializer(new EnumSerializer<InvoiceStatus>(BsonType.String));
            map.MapProperty(invoice => invoice.IsLive);
            map.MapProperty(invoice => invoice.FailureStage);
            map.MapProperty(invoice => invoice.FailureReason);
            map.MapProperty(invoice => invoice.StatusChangedAt);
            map.MapProperty(invoice => invoice.IssueDate).SetSerializer(new NullableSerializer<DateTime>(new DateTimeSerializer(DateTimeKind.Utc)));
            map.MapProperty(invoice => invoice.Currency);
            map.MapProperty(invoice => invoice.SupplierName);
            map.MapProperty(invoice => invoice.SupplierTaxId);
            map.MapProperty(invoice => invoice.CustomerName);
            map.MapProperty(invoice => invoice.CustomerTaxId);
            map.MapProperty(invoice => invoice.Lines);
            map.MapProperty(invoice => invoice.Total).SetSerializer(new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            map.MapProperty(invoice => invoice.UploadPath);
            map.MapProperty(invoice => invoice.ExtractedPath);
            map.MapProperty(invoice => invoice.DraftPath);
            map.MapProperty(invoice => invoice.SignedPath);
            map.MapProperty(invoice => invoice.ApprovedAt);
            map.MapProperty(invoice => invoice.ApprovedBy);
            map.MapProperty(invoice => invoice.RejectedAt);
            map.MapProperty(invoice => invoice.RejectedBy);
            map.MapProperty(invoice => invoice.RejectionReason);
            map.MapProperty(invoice => invoice.SignedAt);
            map.MapProperty(invoice => invoice.TenantId);
        });
    }
}
