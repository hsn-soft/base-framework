using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.DependencyInjection;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public abstract class BaseMongoDbContext : IBaseMongoDbContext, ITransientDependency
{
    public IServiceProvider ServiceProvider { get; set; }

    public IMongoModelSource ModelSource { get; set; }

    public IMongoClient Client { get; private set; }

    public IMongoDatabase Database { get; private set; }

    public IClientSessionHandle SessionHandle { get; private set; }

    public virtual IMongoCollection<T> Collection<T>()
    {
        // return Database.GetCollection<T>(GetCollectionName<T>());

        return Database.GetCollection<T>(GetCollectionName(typeof(T)));
    }

    protected internal virtual void CreateModel(IMongoModelBuilder modelBuilder)
    {
    }

    public virtual void InitializeDatabase(IMongoDatabase database, IMongoClient client, IClientSessionHandle sessionHandle)
    {
        Database = database;
        Client = client;
        SessionHandle = sessionHandle;
    }

    public virtual void InitializeCollections(IMongoDatabase database)
    {
        Database = database;
        ModelSource.GetModel(this);
    }

    // protected virtual string GetCollectionName<T>()
    // {
    //     return GetEntityModel<T>().CollectionName;
    // }
    protected virtual string GetCollectionName(Type documentType)
    {
        var attributes = documentType.GetCustomAttributes(typeof(MongoCollectionAttribute), true);

        return ((MongoCollectionAttribute)attributes.FirstOrDefault())?.CollectionName;
    }

    protected virtual IMongoEntityModel GetEntityModel<TEntity>()
    {
        var model = ModelSource.GetModel(this).Entities.GetOrDefault(typeof(TEntity));

        if (model == null)
        {
            throw new BaseException("Could not find a model for given entity type: " + typeof(TEntity).AssemblyQualifiedName);
        }

        return model;
    }
}