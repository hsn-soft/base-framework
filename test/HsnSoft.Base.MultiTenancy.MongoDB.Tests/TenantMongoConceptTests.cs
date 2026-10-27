using HsnSoft.Base.Data;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HsnSoft.Base.MultiTenancy.MongoDB.Tests;

public class TenantMongoConceptTests
{
    private static readonly Guid s_tenantA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid s_tenantB = new("22222222-2222-2222-2222-222222222222");

    public sealed class Owned : IMultiTenant
    {
        public Guid TenantId { get; set; }
    }

    public sealed class Plain
    {
        public string? Name { get; set; }
    }

    private sealed class FakeTenant(TenantContext? tenant) : ITenantSource
    {
        public TenantContext? Current => tenant;
    }

    private static ServiceProvider Build(TenantContext? tenant = null)
    {
        IServiceCollection services = new ServiceCollection().AddLogging();
        services.AddSingleton<ITenantSource>(new FakeTenant(tenant));
        services.AddBaseMongo(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Mongo"] = "mongodb://localhost:27099/acme" }).Build());
        services.AddBaseMultiTenancyMongo();
        return services.BuildServiceProvider();
    }

    private static string Render<T>(FilterDefinition<T> filter)
        => filter.Render(new RenderArgs<T>(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)).ToJson(new global::MongoDB.Bson.IO.JsonWriterSettings { OutputMode = global::MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson });

    [Fact]
    public void AQuery_HidesTheTenantsTheCallerMayNotSee()
    {
        using ServiceProvider provider = Build(new TenantContext(s_tenantA, AllowedTenantIds: [s_tenantB]));

        FilterDefinition<Owned> filter = provider.GetRequiredService<MongoEntityConcepts>().Visible(Builders<Owned>.Filter.Empty);
        string json = Render(filter);

        Assert.Contains("TenantId", json, StringComparison.Ordinal);
        Assert.Contains("EREREREREREREREREREREQ==", json, StringComparison.Ordinal); // tenant A, standard binary
        Assert.Contains("IiIiIiIiIiIiIiIiIiIiIg==", json, StringComparison.Ordinal); // tenant B
    }

    [Fact]
    public void TheSystemTenant_IsNotNarrowedByTenant_AndASwitchedOffFilterIsNotApplied()
    {
        using ServiceProvider provider = Build(new TenantContext(s_tenantA, IsSystemTenant: true));
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Assert.DoesNotContain("TenantId", Render(concepts.Visible<Owned>()), StringComparison.Ordinal);

        using ServiceProvider ordinary = Build(new TenantContext(s_tenantA));
        using (ordinary.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            Assert.DoesNotContain("TenantId", Render(ordinary.GetRequiredService<MongoEntityConcepts>().Visible<Owned>()), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WithoutATenant_NothingOfATenantMatches()
    {
        using ServiceProvider provider = Build();

        string json = Render(provider.GetRequiredService<MongoEntityConcepts>().Visible<Owned>());

        Assert.Contains("\"TenantId\" : { \"$in\" : [] }", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ADocumentThatIsNotMultiTenant_IsNotFilteredByTenant()
    {
        using ServiceProvider provider = Build(new TenantContext(s_tenantA));

        string json = Render(provider.GetRequiredService<MongoEntityConcepts>().Visible<Plain>());

        Assert.DoesNotContain("TenantId", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AnInsert_GetsTheTenant_AnEntityWithATenantKeepsIt_AndOtherDocumentsAreLeftAlone()
    {
        using ServiceProvider provider = Build(new TenantContext(s_tenantA));
        MongoEntityConcepts concepts = provider.GetRequiredService<MongoEntityConcepts>();
        Owned fresh = new();
        Owned owned = new() { TenantId = s_tenantB };

        concepts.Created(fresh);
        concepts.Created(owned);
        concepts.Created(new Plain());

        Assert.Equal(s_tenantA, fresh.TenantId);
        Assert.Equal(s_tenantB, owned.TenantId);
    }

    [Fact]
    public void AnInsertWithoutATenant_LeavesTheTenantEmpty()
    {
        using ServiceProvider provider = Build();
        Owned fresh = new();

        provider.GetRequiredService<MongoEntityConcepts>().Created(fresh);

        Assert.Equal(Guid.Empty, fresh.TenantId);
    }

    [Fact]
    public void TheRegistration_AddsTheConceptOnce_AndRejectsNull()
    {
        ServiceCollection services = new();
        services.AddBaseMultiTenancyMongo().AddBaseMultiTenancyMongo();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMongoEntityConcept));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseMultiTenancyMongo());
        using ServiceProvider provider = Build(new TenantContext(s_tenantA));
        Assert.Throws<ArgumentNullException>(() => provider.GetServices<IMongoEntityConcept>().Single().Created(null!));
    }
}
