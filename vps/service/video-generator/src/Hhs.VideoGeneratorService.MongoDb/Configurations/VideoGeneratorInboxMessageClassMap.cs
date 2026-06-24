using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.VideoGeneratorService.MongoDb.Configurations;

public static class VideoGeneratorInboxMessageClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<EventInboxMessage>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.EventName).SetIsRequired(true);
            map.MapMember(x => x.Payload).SetIsRequired(true);
            map.MapMember(x => x.Status).SetIsRequired(true);
        });
    }
}
