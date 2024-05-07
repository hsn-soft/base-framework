using System;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IMongoGenericRepository<out TDbContext, TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TDbContext : MongoDbContext
    where TEntity : class, IEntity<TKey>
{
    TDbContext GetDbContext();

    IMongoCollection<TEntity> GetCollection();

    IMongoQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IMongoQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IMongoQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}