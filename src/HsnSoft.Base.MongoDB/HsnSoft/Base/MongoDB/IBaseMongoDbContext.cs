using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public interface IBaseMongoDbContext
{
    IMongoClient Client { get; }

    IMongoDatabase Database { get; }

    IMongoCollection<T> Collection<T>();

    IClientSessionHandle SessionHandle { get; }
}