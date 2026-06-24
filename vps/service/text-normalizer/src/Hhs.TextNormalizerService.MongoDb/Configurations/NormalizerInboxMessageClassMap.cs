using Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class NormalizerInboxMessageClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(NormalizerInboxMessage)))
        {
            BsonClassMap.RegisterClassMap<NormalizerInboxMessage>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                map.MapIdMember(x => x.Id);
            });
        }
    }
}
