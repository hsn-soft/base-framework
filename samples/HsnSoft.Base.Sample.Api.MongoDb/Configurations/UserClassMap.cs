using HsnSoft.Base.Sample.Api.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.Sample.Api.MongoDb.Configurations;

public static class UserClassMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<User>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapMember(x => x.Email).SetIsRequired(true);
        });
    }
}