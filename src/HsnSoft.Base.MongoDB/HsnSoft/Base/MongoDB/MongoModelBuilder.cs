using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public class MongoModelBuilder : IMongoModelBuilder
{
    private static readonly object SyncObj = new object();
    private readonly Dictionary<Type, object> _entityModelBuilders;

    public MongoModelBuilder()
    {
        _entityModelBuilders = new Dictionary<Type, object>();
    }

    public virtual void Entity<TEntity>(Action<IMongoEntityModelBuilder<TEntity>> buildAction = null)
    {
        var model = (IMongoEntityModelBuilder<TEntity>)_entityModelBuilders.GetOrAdd(
            typeof(TEntity),
            () => new MongoEntityModelBuilder<TEntity>()
        );

        buildAction?.Invoke(model);
    }

    public virtual void Entity(Type entityType, Action<IMongoEntityModelBuilder> buildAction = null)
    {
        Check.NotNull(entityType, nameof(entityType));

        var model = (IMongoEntityModelBuilder)_entityModelBuilders.GetOrAdd(
            entityType,
            () => (IMongoEntityModelBuilder)Activator.CreateInstance(
                typeof(MongoEntityModelBuilder<>).MakeGenericType(entityType)
            )
        );

        buildAction?.Invoke(model);
    }

    public virtual IReadOnlyList<IMongoEntityModel> GetEntities()
    {
        return _entityModelBuilders.Values.Cast<IMongoEntityModel>().ToImmutableList();
    }

    public virtual MongoDbContextModel Build(BaseMongoDbContext dbContext)
    {
        lock (SyncObj)
        {
            var useBaseClockHandleDateTime = dbContext.ServiceProvider.GetRequiredService<IOptions<BaseMongoDbOptions>>().Value.UseBaseClockHandleDateTime;

            var entityModels = _entityModelBuilders
                .Select(x => x.Value)
                .Cast<IMongoEntityModel>()
                .ToImmutableDictionary(x => x.EntityType, x => x);

            var baseClasses = new List<Type>();

            foreach (var entityModel in entityModels.Values)
            {
                var map = entityModel.As<IHasBsonClassMap>().GetMap();

                if (useBaseClockHandleDateTime)
                {
                    var dateTimePropertyInfos = entityModel.EntityType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(property =>
                            (property.PropertyType == typeof(DateTime) ||
                             property.PropertyType == typeof(DateTime?)) &&
                            property.CanWrite
                        ).ToList();

                    dateTimePropertyInfos.ForEach(property =>
                    {
                        var disableDateTimeNormalization =
                            entityModel.EntityType.IsDefined(typeof(DisableDateTimeNormalizationAttribute), true) ||
                            ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<DisableDateTimeNormalizationAttribute>(property) != null;

                        if (property.PropertyType == typeof(DateTime?))
                        {
                            map.MapProperty(property.Name).SetSerializer(new NullableSerializer<DateTime>().WithSerializer(new BaseMongoDbDateTimeSerializer(GetDateTimeKind(dbContext), disableDateTimeNormalization)));
                        }
                        else
                        {
                            map.MapProperty(property.Name).SetSerializer(new BaseMongoDbDateTimeSerializer(GetDateTimeKind(dbContext), disableDateTimeNormalization));
                        }
                    });
                }

                if (!BsonClassMap.IsClassMapRegistered(map.ClassType))
                {
                    BsonClassMap.RegisterClassMap(map);
                }

                baseClasses.AddRange(entityModel.EntityType.GetBaseClasses(includeObject: false));

                CreateCollectionIfNotExists(dbContext, entityModel.CollectionName);
            }

            baseClasses = baseClasses.Distinct().ToList();

            foreach (var baseClass in baseClasses)
            {
                if (!BsonClassMap.IsClassMapRegistered(baseClass))
                {
                    var map = new BsonClassMap(baseClass);
                    map.ConfigureBaseConventions();

                    if (useBaseClockHandleDateTime)
                    {
                        var dateTimePropertyInfos = baseClass.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .Where(property =>
                                (property.PropertyType == typeof(DateTime) ||
                                 property.PropertyType == typeof(DateTime?)) &&
                                property.CanWrite
                            ).ToList();

                        dateTimePropertyInfos.ForEach(property =>
                        {
                            if (property.PropertyType == typeof(DateTime?))
                            {
                                map.MapProperty(property.Name).SetSerializer(new NullableSerializer<DateTime>().WithSerializer(new BaseMongoDbDateTimeSerializer(GetDateTimeKind(dbContext), false)));
                            }
                            else
                            {
                                map.MapProperty(property.Name).SetSerializer(new BaseMongoDbDateTimeSerializer(GetDateTimeKind(dbContext), false));
                            }
                        });
                    }

                    BsonClassMap.RegisterClassMap(map);
                }
            }

            return new MongoDbContextModel(entityModels);
        }
    }

    private DateTimeKind GetDateTimeKind(BaseMongoDbContext dbContext)
    {
        return dbContext.ServiceProvider.GetRequiredService<IOptions<BaseClockOptions>>().Value.Kind;
    }

    protected virtual void CreateCollectionIfNotExists(BaseMongoDbContext dbContext, string collectionName)
    {
        var filter = new BsonDocument("name", collectionName);
        var options = new ListCollectionNamesOptions { Filter = filter };

        if (!dbContext.Database.ListCollectionNames(options).Any())
        {
            dbContext.Database.CreateCollection(collectionName);
        }
    }
}