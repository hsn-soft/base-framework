using Abc.ShipmentService.Domain.PaymentDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Abc.ShipmentService.MongoDB.Configurations;

/// <summary>How a <see cref="Payment"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class PaymentClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Payment)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<Payment>(map =>
        {
            map.MapProperty(payment => payment.OrderId);
            map.MapProperty(payment => payment.CustomerId);
            map.MapProperty(payment => payment.Amount).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(payment => payment.Currency);
            map.MapProperty(payment => payment.Status).SetSerializer(new EnumSerializer<PaymentStatus>(BsonType.String));
            map.MapProperty(payment => payment.Reference);
            map.MapProperty(payment => payment.FailureReason);
        });
    }
}
