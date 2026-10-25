using Abc.ProductService.Domain.ProductDomain.Entities;
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

        BsonClassMap.RegisterClassMap<Product>(map =>
        {
            map.MapIdProperty(product => product.Id);
            map.MapProperty(product => product.Name);
            map.MapProperty(product => product.NormalizedName);
            map.MapProperty(product => product.Sku);
            map.MapProperty(product => product.Description);
            map.MapProperty(product => product.Price).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapProperty(product => product.Stock);
            map.MapProperty(product => product.IsActive);
            map.MapProperty(product => product.TenantId);
            map.MapProperty(product => product.CreatedAt);
            map.MapProperty(product => product.CreatedBy);
            map.MapProperty(product => product.UpdatedAt);
            map.MapProperty(product => product.UpdatedBy);
            map.MapProperty(product => product.IsDeleted);
            map.MapProperty(product => product.DeletedAt);
            map.MapProperty(product => product.DeletedBy);
        });
    }
}
