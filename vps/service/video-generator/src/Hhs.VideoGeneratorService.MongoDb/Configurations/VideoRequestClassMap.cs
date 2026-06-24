using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
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
            map.MapMember(x => x.RefContentId).SetIsRequired(true);
            map.MapMember(x => x.RefContentType).SetIsRequired(true);
            map.MapMember(x => x.SourceEventId).SetIsRequired(true);
            map.MapMember(x => x.Status).SetIsRequired(true);
            map.MapMember(x => x.CurrentStep).SetIsRequired(true);
            map.MapMember(x => x.MediaInputJson).SetIsRequired(true);
            map.MapMember(x => x.VideoProviderKey).SetIsRequired(true);
        });
    }
}
