using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.MongoDB;
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

        // the id and the audit fields are the base classes' (their maps first); this map has what is the shipment's own
        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<Shipment>(map =>
        {
            map.MapProperty(shipment => shipment.OrderId);
            map.MapProperty(shipment => shipment.TrackingNumber);
            map.MapProperty(shipment => shipment.Carrier);
            map.MapProperty(shipment => shipment.NormalizedCarrier);
            map.MapProperty(shipment => shipment.Address);
            map.MapProperty(shipment => shipment.Status).SetSerializer(new EnumSerializer<ShipmentStatus>(BsonType.String));
        });
    }
}
