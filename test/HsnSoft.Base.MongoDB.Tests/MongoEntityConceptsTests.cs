using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoEntityConceptsTests
{
    private static readonly Guid s_tenantA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid s_tenantB = new("22222222-2222-2222-2222-222222222222");

    public sealed class Full : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
    {
        public Guid TenantId { get; set; }

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

        public Guid? TenantId { get; init; }

        public bool IsSystemTenant { get; init; }

        public IReadOnlyList<Guid> AllowedTenantIds { get; init; } = [];
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
    public void AQuery_HidesTheDeleted_AndTheTenantsTheCallerMayNotSee()
    {
        using ServiceProvider provider = Build(new FakeUser { Id = Guid.NewGuid(), TenantId = s_tenantA, AllowedTenantIds = [s_tenantB] });

        FilterDefinition<Full> filter = provider.GetRequiredService<MongoEntityConcepts>().Visible(Builders<Full>.Filter.Eq(entity => entity.CreatedBy, null));
        string json = Render(filter);

        Assert.Contains("\"IsDeleted\" : false", json, StringComparison.Ordinal);
        Assert.Contains("TenantId", json, StringComparison.Ordinal);
        Assert.Contains("CreatedBy", json, StringComparison.Ordinal);
        Assert.Contains("EREREREREREREREREREREQ==", json, StringComparison.Ordinal); // tenant A, standard binary
        Assert.Contains("IiIiIiIiIiIiIiIiIiIiIg==", json, StringComparison.Ordinal); // tenant B
    }

    [Fact]
    public void TheSystemTenant_IsNotNarrowedByTenant_AndASwitchedOffFilterIsNotApplied()
    {
        using ServiceProvider provider = Build(new FakeUser { TenantId = s_tenantA, IsSystemTenant = true });
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();

        string system = Render(concepts.Visible<Full>());
        Assert.DoesNotContain("TenantId", system, StringComparison.Ordinal);
        Assert.Contains("IsDeleted", system, StringComparison.Ordinal);

        using (provider.GetRequiredService<IDataFilter>().Disable<ISoftDelete>())
        {
            Assert.DoesNotContain("IsDeleted", Render(concepts.Visible<Full>()), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WithoutATenant_NothingOfATenantMatches()
    {
        using ServiceProvider provider = Build();

        string json = Render(provider.GetRequiredService<MongoEntityConcepts>().Visible<Full>());

        Assert.Contains("\"TenantId\" : { \"$in\" : [] }", json, StringComparison.Ordinal);
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
    public void AnInsert_GetsTheAuditFields_AndTheTenant_AnAnEntityWithATenantKeepsIt()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user, TenantId = s_tenantA });
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Full fresh = new();
        Full owned = new() { TenantId = s_tenantB };

        concepts.Created(fresh);
        concepts.Created(owned);
        concepts.Created(new Plain());

        Assert.Equal((user, user, s_tenantA), (fresh.CreatedBy, fresh.UpdatedBy, fresh.TenantId));
        Assert.Equal(s_tenantB, owned.TenantId);
    }

    [Fact]
    public void AReplace_IsAModification()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user, TenantId = s_tenantA });
        Full entity = new();

        provider.GetRequiredService<MongoEntityConcepts>().Modified(entity);

        Assert.Equal(user, entity.UpdatedBy);
        Assert.Null(entity.CreatedBy);
    }

    [Fact]
    public void ADelete_IsSoftForAnISoftDeleteEntity_AndRealForAnyOther()
    {
        Guid user = Guid.NewGuid();
        using ServiceProvider provider = Build(new FakeUser { Id = user, TenantId = s_tenantA });
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
