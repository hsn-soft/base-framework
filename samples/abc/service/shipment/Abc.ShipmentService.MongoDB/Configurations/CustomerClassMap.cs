using Abc.ShipmentService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson.Serialization;

namespace Abc.ShipmentService.MongoDB.Configurations;

/// <summary>How a <see cref="Customer"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class CustomerClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Customer)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<Customer>(map =>
        {
            map.MapProperty(customer => customer.UserId);
            map.MapProperty(customer => customer.UserName);
            map.MapProperty(customer => customer.NormalizedUserName);
            map.MapProperty(customer => customer.FullName);
            map.MapProperty(customer => customer.NormalizedFullName);
            map.MapProperty(customer => customer.Email);
            map.MapProperty(customer => customer.Phone);
            map.MapProperty(customer => customer.Address);
            map.MapProperty(customer => customer.TenantId);
        });
    }
}
