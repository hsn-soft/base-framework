using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class NormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<NormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ClientId).SetIsRequired(true);
            map.MapMember(x => x.AppContentId).SetIsRequired(true);
            map.MapMember(x => x.DomainName).SetIsRequired(true);
            map.MapMember(x => x.DomainPath).SetIsRequired(true);
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(ScrapingContentDataModel)))
        {
            BsonClassMap.RegisterClassMap<ScrapingContentDataModel>();
        }
    }
}