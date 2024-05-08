using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IMongoGenericRepository<TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    IMongoCollection<TEntity> GetCollection(TEntity entity = null, MongoEntityEventState eventState = MongoEntityEventState.Unchanged);

    IQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}