using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Reflection;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public class MongoModelSource : IMongoModelSource, ISingletonDependency
{
    protected readonly ConcurrentDictionary<Type, MongoDbContextModel> ModelCache = new ConcurrentDictionary<Type, MongoDbContextModel>();

    public virtual MongoDbContextModel GetModel(BaseMongoDbContext dbContext)
    {
        return ModelCache.GetOrAdd(
            dbContext.GetType(),
            _ => CreateModel(dbContext)
        );
    }

    protected virtual MongoDbContextModel CreateModel(BaseMongoDbContext dbContext)
    {
        var modelBuilder = CreateModelBuilder();
        BuildModelFromDbContextType(modelBuilder, dbContext.GetType());
        BuildModelFromDbContextInstance(modelBuilder, dbContext);
        return modelBuilder.Build(dbContext);
    }

    protected virtual MongoModelBuilder CreateModelBuilder()
    {
        return new MongoModelBuilder();
    }

    protected virtual void BuildModelFromDbContextType(IMongoModelBuilder modelBuilder, Type dbContextType)
    {
        var collectionProperties =
            from property in dbContextType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where
                ReflectionHelper.IsAssignableToGenericType(property.PropertyType, typeof(IMongoCollection<>)) &&
                typeof(IEntity).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0])
            select property;

        foreach (var collectionProperty in collectionProperties)
        {
            BuildModelFromDbContextCollectionProperty(modelBuilder, collectionProperty);
        }
    }

    protected virtual void BuildModelFromDbContextCollectionProperty(IMongoModelBuilder modelBuilder, PropertyInfo collectionProperty)
    {
        var entityType = collectionProperty.PropertyType.GenericTypeArguments[0];
        var collectionAttribute = collectionProperty.GetCustomAttributes().OfType<MongoCollectionAttribute>().FirstOrDefault();

        modelBuilder.Entity(entityType, b =>
        {
            b.CollectionName = collectionAttribute?.CollectionName ?? collectionProperty.Name;
        });
    }

    protected virtual void BuildModelFromDbContextInstance(IMongoModelBuilder modelBuilder, BaseMongoDbContext dbContext)
    {
        dbContext.CreateModel(modelBuilder);
    }
}