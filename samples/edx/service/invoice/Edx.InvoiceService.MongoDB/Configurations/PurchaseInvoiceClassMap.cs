using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.InvoiceService.MongoDB.Configurations;

/// <summary>How a <see cref="PurchaseInvoice"/> is stored: the domain has no attributes of the driver, the mapping is here. Amounts are decimals of the database (<c>Decimal128</c>).</summary>
public static class PurchaseInvoiceClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(PurchaseInvoice)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        InvoiceValueMaps.Register();
        BsonClassMap.RegisterClassMap<PurchaseInvoice>(map =>
        {
            map.MapProperty(invoice => invoice.InvoiceUuid);
            map.MapProperty(invoice => invoice.EnvelopeId);
            map.MapProperty(invoice => invoice.Number);
            map.MapProperty(invoice => invoice.NormalizedNumber);
            map.MapProperty(invoice => invoice.IssueDate).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            map.MapProperty(invoice => invoice.Currency);
            map.MapProperty(invoice => invoice.Profile);
            map.MapProperty(invoice => invoice.SupplierRegisterNumber);
            map.MapProperty(invoice => invoice.SupplierName);
            map.MapProperty(invoice => invoice.CustomerRegisterNumber);
            map.MapProperty(invoice => invoice.CustomerName);
            map.MapProperty(invoice => invoice.LineCount);
            map.MapProperty(invoice => invoice.TaxExclusiveAmount).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(invoice => invoice.TaxInclusiveAmount).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(invoice => invoice.PayableAmount).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(invoice => invoice.XmlPath);
            map.MapProperty(invoice => invoice.ReceivedAt);
            map.MapProperty(invoice => invoice.EnvelopeStatusCode);
            map.MapProperty(invoice => invoice.EnvelopeStatusText);
            map.MapProperty(invoice => invoice.EnvelopeStatusAt);
            map.MapProperty(invoice => invoice.AnswerRequestedAt);
            map.MapProperty(invoice => invoice.History);
            map.MapProperty(invoice => invoice.Responses);
            map.MapProperty(invoice => invoice.TenantId);
        });
    }
}
