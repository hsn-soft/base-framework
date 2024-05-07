using HsnSoft.Base.MongoDBOld.Base;

namespace HsnSoft.Base.MongoDBOld.Repository;

public interface IMongoRepository<TDocument> : IBaseRepository<TDocument>
    where TDocument : IBaseDocument
{
}