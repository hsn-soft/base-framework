using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class CustomerContentNormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerContentNormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapIdMember(x => x.Id);
        });
    }
}