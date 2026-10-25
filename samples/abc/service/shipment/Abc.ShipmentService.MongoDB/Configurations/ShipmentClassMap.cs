using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Abc.ShipmentService.MongoDB.Configurations;

/// <summary>How a <see cref="Shipment"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class ShipmentClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Shipment)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Shipment>(map =>
        {
            map.MapIdProperty(shipment => shipment.Id);
            map.MapProperty(shipment => shipment.OrderId);
            map.MapProperty(shipment => shipment.TrackingNumber);
            map.MapProperty(shipment => shipment.Carrier);
            map.MapProperty(shipment => shipment.NormalizedCarrier);
            map.MapProperty(shipment => shipment.Address);
            map.MapProperty(shipment => shipment.Status).SetSerializer(new EnumSerializer<ShipmentStatus>(BsonType.String));
            map.MapProperty(shipment => shipment.TenantId);
            map.MapProperty(shipment => shipment.CreatedAt);
            map.MapProperty(shipment => shipment.CreatedBy);
            map.MapProperty(shipment => shipment.UpdatedAt);
            map.MapProperty(shipment => shipment.UpdatedBy);
            map.MapProperty(shipment => shipment.IsDeleted);
            map.MapProperty(shipment => shipment.DeletedAt);
            map.MapProperty(shipment => shipment.DeletedBy);
        });
    }
}
