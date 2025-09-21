using System.Linq;
using HsnSoft.Base.Domain.Entities;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories;

public interface IMongoGenericRepository< TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    IMongoCollection<TEntity> GetCollection();
    IQueryable<TEntity> GetQueryable();
}