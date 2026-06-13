using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class VideoRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<VideoRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);
            map.MapMember(x => x.DomainName).SetIsRequired(true);
        });
    }
}