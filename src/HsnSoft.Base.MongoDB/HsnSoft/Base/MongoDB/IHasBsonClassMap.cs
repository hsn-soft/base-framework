using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.MongoDB;

public interface IHasBsonClassMap
{
    BsonClassMap GetMap();
}