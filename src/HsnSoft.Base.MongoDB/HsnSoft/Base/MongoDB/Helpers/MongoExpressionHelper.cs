using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Helpers;

public static class MongoExpressionHelper
{
    public static FilterDefinition<TEntity> ToFilter<TEntity>(Expression<Func<TEntity, bool>> predicate)
    {
        return Builders<TEntity>.Filter.Where(predicate);
    }
}