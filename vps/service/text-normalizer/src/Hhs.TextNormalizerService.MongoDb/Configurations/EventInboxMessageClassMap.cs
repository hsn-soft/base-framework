using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class EventInboxMessageClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(EventInboxMessage)))
        {
            BsonClassMap.RegisterClassMap<EventInboxMessage>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                map.MapIdMember(x => x.Id);
            });
        }
    }
}
