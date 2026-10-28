using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.InvoiceService.MongoDB.Configurations;

/// <summary>How an <see cref="InvoiceReport"/> is stored.</summary>
public static class InvoiceReportClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(InvoiceReport)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<InvoiceReport>(map =>
        {
            map.MapProperty(report => report.Name);
            map.MapProperty(report => report.PeriodStart).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            map.MapProperty(report => report.PeriodEnd).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            map.MapProperty(report => report.Currency);
            map.MapProperty(report => report.InvoiceCount);
            map.MapProperty(report => report.ApprovedCount);
            map.MapProperty(report => report.TotalAmount).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(report => report.GeneratedAt);
            map.MapProperty(report => report.TenantId);
        });
    }
}
