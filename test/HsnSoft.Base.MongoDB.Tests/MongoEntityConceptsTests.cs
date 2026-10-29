using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoEntityConceptsTests
{
    public sealed class Full : IFullAuditedObject, IMarkCreated, IMarkModified, IMarkDeleted
    {
        public DateTimeOffset CreatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }

        public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

        public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

        public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
    }

    public sealed class Plain
    {
        public string? Name { get; set; }
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;

        public Guid? Id { get; init; }

        public string? UserName => null;
    }

    private static ServiceProvider Build(FakeUser? user = null)
    {
        IServiceCollection services = new ServiceCollection().AddLogging();
        if (user is not null)
        {
            services.AddSingleton<ICurrentUser>(user);
        }

        return services.AddBaseMongo(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Mongo"] = "mongodb://localhost:27099/acme" }).Build()).BuildServiceProvider();
    }

    private static string Render<T>(FilterDefinition<T> filter)
        => filter.Render(new RenderArgs<T>(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)).ToJson(new global::MongoDB.Bson.IO.JsonWriterSettings { OutputMode = global::MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson });

    [Fact]
    public void AQuery_HidesTheDeleted_AndASwitchedOffFilterIsNotApplied()
    {
        using ServiceProvider provider = Build(new FakeUser { Id = Guid.NewGuid() });
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();

        string json = Render(concepts.Visible(Builders<Full>.Filter.Eq(entity => entity.CreatedBy, null)));
        Assert.Contains("\"IsDeleted\" : false", json, StringComparison.Ordinal);
        Assert.Contains("CreatedBy", json, StringComparison.Ordinal);
        Assert.DoesNotContain("TenantId", json, StringComparison.Ordinal);

        using (provider.GetRequiredService<IDataFilter>().Disable<ISoftDelete>())
        {
            Assert.DoesNotContain("IsDeleted", Render(concepts.Visible<Full>()), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AConceptOfAPackage_NarrowsTheQuery_AndWritesIntoAnInsertedDocument()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddSingleton<IMongoEntityConcept, MarkingConcept>();
        using ServiceProvider provider = services.AddBaseMongo(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Mongo"] = "mongodb://localhost:27099/acme" }).Build()).BuildServiceProvider();
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Plain plain = new();

        string json = Render(concepts.Visible<Plain>());
        concepts.Created(plain);

        Assert.Contains("Name", json, StringComparison.Ordinal);
        Assert.Equal("created", plain.Name);
    }

    private sealed class MarkingConcept : IMongoEntityConcept
    {
        public FilterDefinition<T>? Visible<T>() => typeof(T) == typeof(Plain) ? Builders<T>.Filter.Eq("Name", "visible") : null;

        public void Created(object entity)
        {
            if (entity is Plain plain)
            {
                plain.Name = "created";
            }
        }
    }

    [Fact]
    public void ADocumentWithNoConcepts_IsNotFiltered()
    {
        using ServiceProvider provider = Build();
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();

        Assert.Equal("{ }", Render(concepts.Visible<Plain>()).Replace("\r", string.Empty).Replace("\n", string.Empty));
        Assert.Contains("Name", Render(concepts.Visible(Builders<Plain>.Filter.Eq(plain => plain.Name, "x"))), StringComparison.Ordinal);
    }

    [Fact]
    public void AnInsert_GetsTheAuditFields()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user });
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Full fresh = new();

        concepts.Created(fresh);
        concepts.Created(new Plain());

        Assert.Equal((user, user), (fresh.CreatedBy, fresh.UpdatedBy));
    }

    [Fact]
    public void AReplace_IsAModification()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user });
        Full entity = new();

        provider.GetRequiredService<MongoEntityConcepts>().Modified(entity);

        Assert.Equal(user, entity.UpdatedBy);
        Assert.Null(entity.CreatedBy);
    }

    [Fact]
    public void ADelete_IsSoftForAnISoftDeleteEntity_AndRealForAnyOther()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user });
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Full entity = new();

        Assert.True(concepts.SoftDeleted(entity));
        Assert.False(concepts.SoftDeleted(new Plain()));

        Assert.True(entity.IsDeleted);
        Assert.Equal(user, entity.DeletedBy);
    }

    [Fact]
    public void TheMethods_RejectNull()
    {
        using ServiceProvider provider = Build();
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();

        Assert.Throws<ArgumentNullException>(() => concepts.Created(null!));
        Assert.Throws<ArgumentNullException>(() => concepts.Modified(null!));
        Assert.Throws<ArgumentNullException>(() => concepts.SoftDeleted(null!));
    }
}
