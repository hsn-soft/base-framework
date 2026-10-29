using Abc.ProductService.Domain.ProductDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Abc.ProductService.MongoDB.Configurations;

/// <summary>How a <see cref="Product"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class ProductClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Product)))
        {
            return;
        }

        // the id and the audit fields are the base classes' (their maps first); this map has what is the product's own
        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<Product>(map =>
        {
            map.MapProperty(product => product.Name);
            map.MapProperty(product => product.NormalizedName);
            map.MapProperty(product => product.Sku);
            map.MapProperty(product => product.Description);
            map.MapProperty(product => product.Price).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(product => product.Stock);
            map.MapProperty(product => product.IsActive);
            map.MapProperty(product => product.TenantId);
        });
    }
}
