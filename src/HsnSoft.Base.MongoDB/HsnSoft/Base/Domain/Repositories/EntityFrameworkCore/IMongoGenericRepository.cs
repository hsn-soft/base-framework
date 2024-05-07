using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IMongoGenericRepository<out TDbContext, TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    TDbContext GetDbContext();

    IMongoCollection<TEntity> GetCollection(TEntity entity = null, MongoEntityEventState eventState = MongoEntityEventState.Unchanged);

    IQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}