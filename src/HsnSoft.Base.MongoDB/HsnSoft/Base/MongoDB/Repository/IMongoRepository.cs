using HsnSoft.Base.MongoDB.Base;

namespace HsnSoft.Base.MongoDB.Repository;

public interface IMongoRepository<TDocument> : IBaseRepository<TDocument>
    where TDocument : IBaseDocument
{
}