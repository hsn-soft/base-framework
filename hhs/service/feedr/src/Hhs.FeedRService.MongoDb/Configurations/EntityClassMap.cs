using HsnSoft.Base.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.FeedRService.MongoDb.Configurations;

public static class EntityClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<Entity<Guid>>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapIdMember(x => x.Id);
        });
    }
}