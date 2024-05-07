using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.MongoDB.Attributes;
using HsnSoft.Base.MongoDB.Options;
using HsnSoft.Base.MongoDBOld.Base;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ReadPreference = MongoDB.Driver.ReadPreference;

namespace HsnSoft.Base.MongoDBOld.Repository;

public abstract class BaseRepository<TDocument> : IBaseRepository<TDocument>
    where TDocument : IBaseDocument
{
    private readonly IMongoCollection<TDocument> _collection;
    private const int DefaultQueryExecutionMaxSeconds = 60;
    private QueryOptions _queryOptions;
    protected BaseRepository(IOptions<MongoDbSettings> settings)
    {
        var clientSettings = CreateClientSettings(settings);
        var database = new MongoClient(clientSettings).GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
    }

    protected BaseRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
        _queryOptions = new QueryOptions
        {
            MaxTime = mongoClient.Settings.WaitQueueTimeout,
            MaxAwaitTime = mongoClient.Settings.WaitQueueTimeout
        };
    }

    private MongoClientSettings CreateClientSettings(IOptions<MongoDbSettings> settings)
    {
        ThreadPool.GetMaxThreads(out var maxWt, out var _);
        var clientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        clientSettings.MaxConnectionPoolSize = maxWt * 2;

        var queryExecutionMaxSeconds = settings.Value.QueryExecutionMaxSeconds > 0
            ? TimeSpan.FromSeconds(settings.Value.QueryExecutionMaxSeconds)
            : TimeSpan.FromSeconds(DefaultQueryExecutionMaxSeconds);

        // In version 2.19, MongoDB team upgraded to LinqProvider.V3, rolling back to V2 until LinQ is stable...
        // https://www.mongodb.com/community/forums/t/issue-with-2-18-to-2-19-nuget-upgrade-of-mongodb-c-driver/211894/2
        clientSettings.LinqProvider = LinqProvider.V2;
        clientSettings.WaitQueueTimeout = queryExecutionMaxSeconds;

        _queryOptions = new QueryOptions
        {
            MaxTime = queryExecutionMaxSeconds,
            MaxAwaitTime = queryExecutionMaxSeconds
        };

        return clientSettings;
    }

    private string GetCollectionName(Type documentType)
    {
        return ((BsonCollectionAttribute)documentType.GetCustomAttributes(
                typeof(BsonCollectionAttribute),
                true)
            .FirstOrDefault())?.CollectionName;
    }

    private ReadPreference DecideReadPreference(ReadOption readOption)
    {
        return readOption switch
        {
            ReadOption.Primary => ReadPreference.Primary,
            ReadOption.PrimaryPreferred => ReadPreference.PrimaryPreferred,
            ReadOption.SecondaryPreferred => ReadPreference.SecondaryPreferred,
            ReadOption.Secondary => ReadPreference.Secondary,
            ReadOption.Nearest => ReadPreference.Nearest,
            _ => ReadPreference.Primary
        };
    }

    public virtual IQueryable<TDocument> AsQueryable()
    {
        return _collection.WithReadPreference(ReadPreference.SecondaryPreferred).AsQueryable();
    }

    public async Task<IEnumerable<TDocument>> FilterByAsync(FilterDefinition<TDocument> filterDefinition)
    {
        var res = await _collection.FindAsync(filterDefinition, new FindOptions<TDocument>
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        });
        return res.ToEnumerable();
    }

    public IEnumerable<TDocument> FilterBy(FilterDefinition<TDocument> filterDefinition)
    {
        var res = _collection.Find(filterDefinition, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        });
        return res.ToEnumerable();
    }

    public virtual IEnumerable<TDocument> FilterBy(
        Expression<Func<TDocument, bool>> filterExpression)
    {
        return _collection.Find(filterExpression, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).ToEnumerable();
    }

    public virtual IEnumerable<TDocument> FilterBy(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };

        var find = _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption))
            .Find(filterExpression, new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });

        if (filterOptions.PageSize != null && filterOptions.Page != null)
        {
            find = find.Skip((filterOptions.Page - 1) * filterOptions.PageSize).Limit(filterOptions.PageSize);
        }

        return find.ToEnumerable();
    }

    public virtual IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, TProjected>> projectionExpression)
    {
        return _collection.Find(filterExpression, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).Project(projectionExpression).ToEnumerable();
    }

    public virtual IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        Expression<Func<TDocument, TProjected>> projectionExpression)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };
        var find = _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption))
            .Find(filterExpression, new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });

        if (filterOptions.PageSize != null && filterOptions.Page != null)
        {
            find = find.Skip((filterOptions.Page - 1) * filterOptions.PageSize).Limit(filterOptions.PageSize);
        }

        return find.Project(projectionExpression).ToEnumerable();
    }

    public virtual IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        List<string> include = null, List<string> exclude = null)
    {
        ProjectionDefinition<TDocument, TProjected> projectionDefinition =
            PrepareProjectDefinition<TProjected>(include, exclude);
        return projectionDefinition == null
            ? null
            : _collection.Find(filterExpression, new FindOptions
                {
                    MaxAwaitTime = _queryOptions.MaxAwaitTime,
                    MaxTime = _queryOptions.MaxTime
                }).Project(projectionDefinition)
                .ToEnumerable();
    }

    public virtual IEnumerable<TProjected> FilterBy<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        List<string> include = null, List<string> exclude = null)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };

        ProjectionDefinition<TDocument, TProjected> projectionDefinition =
            PrepareProjectDefinition<TProjected>(include, exclude);

        if (projectionDefinition == null)
        {
            return null;
        }

        var find = _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption))
            .Find(filterExpression, new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });

        if (filterOptions.PageSize != null && filterOptions.Page != null)
        {
            find = find.Skip((filterOptions.Page - 1) * filterOptions.PageSize).Limit(filterOptions.PageSize);
        }

        return find.Project(projectionDefinition).ToEnumerable();
    }

    public async Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        List<string> include = null, List<string> exclude = null)
    {
        ProjectionDefinition<TDocument, TProjected> projectionDefinition =
            PrepareProjectDefinition<TProjected>(include, exclude);
        var result = await _collection.FindAsync(filterExpression, new FindOptions<TDocument, TProjected>
            {
                Projection = projectionDefinition,
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });
        return result.ToEnumerable();
    }

    public async Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        List<string> include = null, List<string> exclude = null)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };

        ProjectionDefinition<TDocument, TProjected> projectionDefinition =
            PrepareProjectDefinition<TProjected>(include, exclude);
        var result = await _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption)).FindAsync(
            filterExpression, new FindOptions<TDocument, TProjected>
            {
                Projection = projectionDefinition,
                Skip = filterOptions.Page != null && filterOptions.PageSize != null
                    ? (filterOptions.Page - 1) * filterOptions.PageSize
                    : default,
                Limit = filterOptions.PageSize,
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });
        return result.ToEnumerable();
    }


    private ProjectionDefinition<TDocument, TProjected> PrepareProjectDefinition<TProjected>(
        List<string> include = null,
        List<string> exclude = null)
    {
        ProjectionDefinition<TDocument> projectionDefinition = null;

        if (include == null && exclude == null)
        {
            return null;
        }

        var firstInclude = include?.FirstOrDefault();
        var firstExclude = exclude?.FirstOrDefault();

        if (!string.IsNullOrEmpty(firstInclude))
        {
            projectionDefinition = Builders<TDocument>.Projection.Include(firstInclude);
        }

        if (!string.IsNullOrEmpty(firstExclude))
        {
            projectionDefinition = projectionDefinition != null
                ? projectionDefinition.Exclude(firstExclude)
                : Builders<TDocument>.Projection.Exclude(firstInclude);
        }

        if (include is { Count: > 1 })
        {
            projectionDefinition = include.Skip(1).ToList()
                .Aggregate(projectionDefinition, (current, s) => current.Include(s));
        }

        if (exclude is { Count: > 1 })
        {
            projectionDefinition = exclude.Skip(1).ToList()
                .Aggregate(projectionDefinition, (current, s) => current.Exclude(s));
        }

        return projectionDefinition;
    }

    public async Task<IEnumerable<TDocument>> FilterByAsync(
        Expression<Func<TDocument, bool>> filterExpression)
    {
        var result = await _collection.FindAsync(filterExpression, new FindOptions<TDocument>
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        });
        return result.ToEnumerable();
    }

    public async Task<IEnumerable<TDocument>> FilterByAsync(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };

        var result = await _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption)).FindAsync(
            filterExpression, new FindOptions<TDocument>
            {
                Skip = filterOptions.Page != null && filterOptions.PageSize != null
                    ? (filterOptions.Page - 1) * filterOptions.PageSize
                    : default,
                Limit = filterOptions.PageSize,
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });
        return result.ToEnumerable();
    }

    public async Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, TProjected>> projectionExpression)
    {
        var result = await _collection.FindAsync(filterExpression, new FindOptions<TDocument, TProjected>
        {
            Projection = new FindExpressionProjectionDefinition<TDocument, TProjected>(projectionExpression),
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        });
        return result.ToEnumerable();
    }

    public async Task<IEnumerable<TProjected>> FilterByAsync<TProjected>(
        Expression<Func<TDocument, bool>> filterExpression, FilterOptions filterOptions,
        Expression<Func<TDocument, TProjected>> projectionExpression)
    {
        filterOptions ??= new FilterOptions { PageSize = null, Page = null, ReadOption = ReadOption.Primary };
        var result = await _collection.WithReadPreference(DecideReadPreference(filterOptions.ReadOption)).FindAsync(
            filterExpression, new FindOptions<TDocument, TProjected>
            {
                Projection = new FindExpressionProjectionDefinition<TDocument, TProjected>(projectionExpression),
                Skip = filterOptions.Page != null && filterOptions.PageSize != null
                    ? (filterOptions.Page - 1) * filterOptions.PageSize
                    : default,
                Limit = filterOptions.PageSize,
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            });
        return result.ToEnumerable();
    }

    public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption)
    {
        return _collection.WithReadPreference(DecideReadPreference(readOption)).Find(filterExpression, new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            })
            .FirstOrDefault();
    }

    public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
    {
        return _collection.Find(filterExpression, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).FirstOrDefault();
    }

    public async Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption)
    {
        return await _collection.WithReadPreference(DecideReadPreference(readOption)).Find(filterExpression, new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
    {
        return await _collection.Find(filterExpression, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).FirstOrDefaultAsync();
    }

    public virtual TDocument FindById(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return _collection.Find(filter, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).SingleOrDefault();
    }

    public virtual TDocument FindById(string id, ReadOption readOption)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return _collection.WithReadPreference(DecideReadPreference(readOption)).Find(filter, new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).SingleOrDefault();
    }

    public async Task<TDocument> FindByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.Find(filter,new FindOptions
        {
            MaxAwaitTime = _queryOptions.MaxAwaitTime,
            MaxTime = _queryOptions.MaxTime
        }).SingleOrDefaultAsync();
    }

    public async Task<TDocument> FindByIdAsync(string id, ReadOption readOption)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.WithReadPreference(DecideReadPreference(readOption)).Find(filter,new FindOptions
            {
                MaxAwaitTime = _queryOptions.MaxAwaitTime,
                MaxTime = _queryOptions.MaxTime
            })
            .SingleOrDefaultAsync();
    }

    public virtual void InsertOne(TDocument document)
    {
        _collection.InsertOne(document);
    }

    public async Task InsertOneAsync(TDocument document)
    {
        await _collection.InsertOneAsync(document);
    }

    public void InsertMany(ICollection<TDocument> documents)
    {
        _collection.InsertMany(documents);
    }

    public virtual async Task InsertManyAsync(ICollection<TDocument> documents)
    {
        await _collection.InsertManyAsync(documents);
    }

    public void ReplaceOne(TDocument document)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        _collection.FindOneAndReplace(filter, document);
    }

    public virtual async Task ReplaceOneAsync(TDocument document)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        await _collection.FindOneAndReplaceAsync(filter, document);
    }

    public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
    {
        _collection.FindOneAndDelete(filterExpression);
    }

    public async Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
    {
        await _collection.FindOneAndDeleteAsync(filterExpression);
    }

    public void DeleteById(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        _collection.FindOneAndDelete(filter);
    }

    public async Task DeleteByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        await _collection.FindOneAndDeleteAsync(filter);
    }

    public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
    {
        _collection.DeleteMany(filterExpression);
    }

    public async Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
    {
        await _collection.DeleteManyAsync(filterExpression);
    }

    public async Task ReplaceManyAsync(List<TDocument> documents, bool isOrdered = false)
    {
        var models = PrepareModelsForReplaceMany(documents);
        await _collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = isOrdered });
    }

    public void ReplaceMany(List<TDocument> documents, bool isOrdered = false)
    {
        var models = PrepareModelsForReplaceMany(documents);
        _collection.BulkWrite(models, new BulkWriteOptions { IsOrdered = isOrdered });
    }

    private List<WriteModel<TDocument>> PrepareModelsForReplaceMany(List<TDocument> documents)
    {
        var models = new List<WriteModel<TDocument>>();
        foreach (var document in documents)
        {
            var filter = new FilterDefinitionBuilder<TDocument>().Where(doc => doc.Id == document.Id);
            models.Add(new ReplaceOneModel<TDocument>(filter, document));
        }

        return models;
    }

    public long Count(Expression<Func<TDocument, bool>> filterExpression)
    {
        return _collection.CountDocuments(filterExpression);
    }

    public long Count(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption)
    {
        return _collection.WithReadPreference(DecideReadPreference(readOption))
            .CountDocuments(filterExpression, new CountOptions
            {
                MaxTime = _queryOptions.MaxTime
            });
    }

    public async Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression)
    {
        return await _collection.CountDocumentsAsync(filterExpression,
            new CountOptions
            {
                MaxTime = _queryOptions.MaxTime
            });
    }

    public long EstimatedDocumentCount()
    {
        return _collection.EstimatedDocumentCount();
    }

    public async Task<long> EstimatedDocumentCountAsync()
    {
        return await _collection.EstimatedDocumentCountAsync();
    }

    public async Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression,
        ReadOption readOption)
    {
        return await _collection.WithReadPreference(DecideReadPreference(readOption))
            .CountDocumentsAsync(filterExpression,new CountOptions
            {
                MaxTime = _queryOptions.MaxTime
            });
    }

    public bool Update(Expression<Func<TDocument, bool>> filterExpression, Dictionary<string, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        return updateDefinition != null && _collection.UpdateOne(filterExpression, updateDefinition).IsAcknowledged;
    }

    public bool Update(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        return updateDefinition != null && _collection.UpdateOne(filterExpression, updateDefinition).IsAcknowledged;
    }

    public async Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        if (updateDefinition == null)
            return;

        await _collection.UpdateOneAsync(filterExpression, updateDefinition);
    }

    public async Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        if (updateDefinition == null)
            return;

        await _collection.UpdateOneAsync(filterExpression, updateDefinition);
    }

    public bool UpdateMany(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        return updateDefinition != null &&
               _collection.UpdateMany(filterExpression, updateDefinition).IsAcknowledged;
    }

    public bool UpdateMany(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        return updateDefinition != null &&
               _collection.UpdateMany(filterExpression, updateDefinition).IsAcknowledged;
    }

    public async Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        if (updateDefinition == null)
            return;
        await _collection.UpdateManyAsync(filterExpression, updateDefinition);
    }

    public async Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updateDefinition = PrepareUpdateDefinition(updateValues);
        if (updateDefinition == null)
            return;
        await _collection.UpdateManyAsync(filterExpression, updateDefinition);
    }

    public async Task UpdateNestedAsync<T>(Expression<Func<TDocument, bool>> filterExpression,
        Expression<Func<TDocument, IEnumerable<T>>> filterField, Expression<Func<T, bool>> filterObject,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updates = new List<UpdateDefinition<TDocument>>();

        var filterBuilder = Builders<TDocument>.Filter;
        var filter = filterBuilder.And(filterExpression) &
                     filterBuilder.ElemMatch(filterField, filterObject);

        foreach (var (key, value) in updateValues)
        {
            updates.Add(Builders<TDocument>.Update.Set(key, value));
        }

        var updateDefinition = Builders<TDocument>.Update.Combine(updates);
        await _collection.UpdateOneAsync(filter, updateDefinition);
    }

    public async Task UpdateNestedFields(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<string, dynamic> updateList, List<string> filterList)
    {
        var filterBuilder = Builders<TDocument>.Filter;
        var filter = filterBuilder.And(filterExpression);
        var update = PrepareUpdateDefinition(updateList);
        var arrayFilter = new List<JsonArrayFilterDefinition<BsonDocument>>();
        foreach (string f in filterList)
        {
            arrayFilter.Add(new JsonArrayFilterDefinition<BsonDocument>(f));
        }

        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilter };
        await _collection.UpdateOneAsync(filter, update, updateOptions);
    }

    public async Task UpdateNestedFields(Expression<Func<TDocument, bool>> filterExpression,
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues, List<string> filterList)
    {
        var filterBuilder = Builders<TDocument>.Filter;
        var filter = filterBuilder.And(filterExpression);
        var update = PrepareUpdateDefinition(updateValues);
        var arrayFilter = new List<JsonArrayFilterDefinition<BsonDocument>>();
        foreach (string f in filterList)
        {
            arrayFilter.Add(new JsonArrayFilterDefinition<BsonDocument>(f));
        }

        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilter };
        await _collection.UpdateOneAsync(filter, update, updateOptions);
    }

    public async Task InsertNestedObjectAsync<T>(Expression<Func<TDocument, bool>> filterExpression, string key,
        List<T> updateList, List<string> filterList)
    {
        var filterBuilder = Builders<TDocument>.Filter;
        var filter = filterBuilder.And(filterExpression);
        var update = PrepareInsertDefinition(key, updateList);
        var arrayFilter = new List<JsonArrayFilterDefinition<BsonDocument>>();
        foreach (string f in filterList)
        {
            arrayFilter.Add(new JsonArrayFilterDefinition<BsonDocument>(f));
        }

        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilter };
        await _collection.UpdateOneAsync(filter, update, updateOptions);
    }

    public async Task DeleteNestedObjectAsync<T>(Expression<Func<TDocument, bool>> filterExpression, string key,
        Expression<Func<T, bool>> filterObject, List<string> filterList)
    {
        var filterBuilder = Builders<TDocument>.Filter;
        var filter = filterBuilder.And(filterExpression);
        var update = PrepareDeleteDefinition(key, filterObject);
        var arrayFilter = new List<JsonArrayFilterDefinition<BsonDocument>>();
        foreach (string f in filterList)
        {
            arrayFilter.Add(new JsonArrayFilterDefinition<BsonDocument>(f));
        }

        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilter };
        await _collection.UpdateOneAsync(filter, update, updateOptions);
    }

    private UpdateDefinition<TDocument> PrepareUpdateDefinition(Dictionary<string, dynamic> updateValues)
    {
        var updates = new List<UpdateDefinition<TDocument>>();

        if (updateValues.Keys.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in updateValues)
        {
            updates.Add(Builders<TDocument>.Update.Set(key, value));
        }

        if (updates.Count == 0)
        {
            return null;
        }

        return Builders<TDocument>.Update.Combine(updates);
    }

    private UpdateDefinition<TDocument> PrepareUpdateDefinition(
        Dictionary<Expression<Func<TDocument, object>>, object> updateValues)
    {
        var updates = new List<UpdateDefinition<TDocument>>();

        if (updateValues.Keys.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in updateValues)
        {
            updates.Add(Builders<TDocument>.Update.Set(key, value));
        }

        if (updates.Count == 0)
        {
            return null;
        }

        return Builders<TDocument>.Update.Combine(updates);
    }

    private UpdateDefinition<TDocument> PrepareInsertDefinition<T>(string key, List<T> updateValues)
    {
        var updates = new List<UpdateDefinition<TDocument>>();

        updates.Add(Builders<TDocument>.Update.PushEach(key, updateValues));

        if (updates.Count == 0)
        {
            return null;
        }

        return Builders<TDocument>.Update.Combine(updates);
    }

    private UpdateDefinition<TDocument> PrepareDeleteDefinition<T>(string key,
        Expression<Func<T, bool>> filterObject)
    {
        var updates = new List<UpdateDefinition<TDocument>>();

        updates.Add(Builders<TDocument>.Update.PullFilter<T>(key, filterObject));

        if (updates.Count == 0)
        {
            return null;
        }

        return Builders<TDocument>.Update.Combine(updates);
    }

    public TDocument FindOneAndUpdate(Expression<Func<TDocument, bool>> filterExpression,
        UpdateDefinition<TDocument> updateDefinition, FindOneAndUpdateOptions<TDocument> updateOptions)
    {
        updateOptions.MaxTime = _queryOptions.MaxTime;

        var result = _collection.FindOneAndUpdate(filterExpression, updateDefinition, updateOptions);
        return result;
    }

    public async Task<TDocument> FindOneAndUpdateAsync(Expression<Func<TDocument, bool>> filterExpression,
        UpdateDefinition<TDocument> updateDefinition, FindOneAndUpdateOptions<TDocument> updateOptions)
    {
        updateOptions.MaxTime = _queryOptions.MaxTime;

        var result = await _collection.FindOneAndUpdateAsync(filterExpression, updateDefinition, updateOptions);
        return result;
    }

    public IEnumerable<TDocument> FilterByText(string searchTerm, string defaultTextIndexLanguage = "en")
    {
        return FilterByTextAsync(searchTerm, defaultTextIndexLanguage).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<TDocument>> FilterByTextAsync(string searchTerm, string defaultTextIndexLanguage = "en")
    {
        return await FilterByTextAsync(new List<string> { searchTerm }, defaultTextIndexLanguage);
    }

    public IEnumerable<TDocument> FilterByText(List<string> searchTerms, string defaultTextIndexLanguage = "en")
    {
        return FilterByTextAsync(searchTerms, defaultTextIndexLanguage).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<TDocument>> FilterByTextAsync(List<string> searchTerms, string defaultTextIndexLanguage = "en")
    {
        var searchText = string.Join(' ', (searchTerms ?? new List<string>())).Trim();

        var filter = Builders<TDocument>.Filter.Text(searchText, new TextSearchOptions
        {
            CaseSensitive = false, DiacriticSensitive = false, Language = defaultTextIndexLanguage
        });

        return await FilterByAsync(filter);
    }

    public async Task<string> CreateTextIndex(IndexKeysDefinition<TDocument> textIndexKeys, bool ifExistReCreate, string defaultTextIndexLanguage = "en")
    {
        var textIndexlist = GetTextIndexList();
        if (textIndexlist is { Count: > 0 } && ifExistReCreate)
        {
            // drop text index because each collection can have one text search index
            foreach (var index in textIndexlist)
            {
                await _collection.Indexes.DropOneAsync(index);
            }
        }

        var textIndexOptions = new CreateIndexOptions
        {
            DefaultLanguage = defaultTextIndexLanguage,
            Name = typeof(TDocument).Name + "_TextSearchIndex"
        };

        return await _collection.Indexes.CreateOneAsync(new CreateIndexModel<TDocument>(textIndexKeys, textIndexOptions));
    }

    private List<string> GetTextIndexList()
    {
        var indexes = _collection.Indexes.List().ToList();
        var indexNames = indexes
            .SelectMany(i => i.Elements)
            .Where(e => string.Equals(e.Name, "name", StringComparison.CurrentCultureIgnoreCase))
            .Select(n => n.Value.ToString()).ToList();

        return indexNames.Where(x => x.EndsWith("_TextSearchIndex") || x.EndsWith("_text")).ToList();
    }
}