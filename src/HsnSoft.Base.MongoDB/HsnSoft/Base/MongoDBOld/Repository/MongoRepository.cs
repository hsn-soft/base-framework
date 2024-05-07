using HsnSoft.Base.MongoDBOld.Base;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDBOld.Repository;

public class MongoRepository<TDocument> : BaseRepository<TDocument>, IMongoRepository<TDocument>
    where TDocument : IBaseDocument
{
    public MongoRepository(IOptions<MongoDbSettings> settings) : base(settings)
    {
    }
}

public class TestMongoRepository<TDocument> : BaseRepository<TDocument>, IMongoRepository<TDocument>
    where TDocument : IBaseDocument
{
    public TestMongoRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings) : base(mongoClient, settings)
    {
    }
}