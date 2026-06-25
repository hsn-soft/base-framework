using HsnSoft.Base.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.EventManagerService.MongoDb.Configurations;

public static class EntityClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<Entity<Guid>>(map =>
        {
            map.AutoMap();
            map.MapIdMember(x => x.Id);
            map.SetIgnoreExtraElements(true);
        });
    }
}