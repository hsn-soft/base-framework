using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.MongoDB;

public static class BaseBsonClassMapExtensions
{
    public static void ConfigureBaseConventions(this BsonClassMap map)
    {
        map.AutoMap();
    }
}