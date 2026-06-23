using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class ContentNormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<ContentNormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);
            map.MapMember(x => x.CustomerContentId).SetIsRequired(true);
            map.MapMember(x => x.DomainName).SetIsRequired(true);
            map.MapMember(x => x.DomainPath).SetIsRequired(true);
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(ScrapingContentDataModel)))
        {
            BsonClassMap.RegisterClassMap<ScrapingContentDataModel>();
        }
    }
}