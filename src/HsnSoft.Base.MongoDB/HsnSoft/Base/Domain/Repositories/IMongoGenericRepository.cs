using System.Linq;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;

namespace HsnSoft.Base.Domain.Repositories;

public interface IMongoGenericRepository< TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    ITrackingMongoCollection<TEntity> GetCollection();
    IQueryable<TEntity> GetQueryable();
}