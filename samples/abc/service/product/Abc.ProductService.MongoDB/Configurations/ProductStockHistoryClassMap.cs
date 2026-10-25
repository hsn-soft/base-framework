using Abc.ProductService.Domain.ProductStockHistoryDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson.Serialization;

namespace Abc.ProductService.MongoDB.Configurations;

/// <summary>How a <see cref="ProductStockHistory"/> is stored: the domain has no attributes of the driver, the mapping is here.</summary>
public static class ProductStockHistoryClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(ProductStockHistory)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<ProductStockHistory>(map =>
        {
            map.MapProperty(history => history.ProductId);
            map.MapProperty(history => history.ProductName);
            map.MapProperty(history => history.NormalizedProductName);
            map.MapProperty(history => history.OldStock);
            map.MapProperty(history => history.NewStock);
            map.MapProperty(history => history.Change);
            map.MapProperty(history => history.Reason);
            map.MapProperty(history => history.Note);
            map.MapProperty(history => history.TenantId);
        });
    }
}
