using Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class CustomerConfigurationClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerConfiguration>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ClientName).SetIsRequired(true);
        });
    }
}