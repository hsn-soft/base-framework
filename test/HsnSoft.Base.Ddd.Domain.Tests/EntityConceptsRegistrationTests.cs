using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Ddd.Domain.Tests;

public class EntityConceptsRegistrationTests
{
    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;

        public Guid? Id { get; init; }

        public string? UserName => null;

        public Guid? TenantId { get; init; }

        public bool IsSystemTenant { get; init; }

        public IReadOnlyList<Guid> AllowedTenantIds { get; init; } = [];
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Product : FullAuditedEntity<Guid>
    {
        public Product()
            : base(Guid.NewGuid())
        {
        }
    }

    private static readonly DateTimeOffset s_t1 = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheRegistration_AddsTheConcepts_AndKeepsWhatTheHostRegistered()
    {
        Guid user = Guid.NewGuid();
        ServiceCollection services = new();
        services.AddSingleton<ICurrentUser>(new FakeUser { Id = user, TenantId = Guid.NewGuid() });
        services.AddSingleton<TimeProvider>(new FixedClock(s_t1));

        using ServiceProvider provider = services.AddBaseEntityConcepts().BuildServiceProvider();

        Product product = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(product);
        Assert.Equal((s_t1, user), (product.CreatedAt, product.CreatedBy));
        Assert.True(provider.GetRequiredService<IDataFilter>().IsEnabled<IMultiTenant>());
        Assert.True(provider.GetRequiredService<ICurrentTenant>().IsAvailable);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEntityConcepts());
    }

    [Fact]
    public void TheRegistration_WorksWithoutACurrentUser()
    {
        using ServiceProvider provider = new ServiceCollection().AddBaseEntityConcepts().BuildServiceProvider();

        Product product = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(product);

        Assert.Null(product.CreatedBy);
        Assert.False(provider.GetRequiredService<ICurrentTenant>().IsAvailable);
    }

    [Fact]
    public void ADeletedEntity_IsMarkedOnce_WithTheTimeAndTheUserOfTheDeletion()
    {
        Guid user = Guid.NewGuid();
        ServiceCollection services = new();
        services.AddSingleton<ICurrentUser>(new FakeUser { Id = user });
        services.AddSingleton<TimeProvider>(new FixedClock(s_t1));
        using ServiceProvider provider = services.AddBaseEntityConcepts().BuildServiceProvider();
        Product product = new();

        provider.GetRequiredService<IAuditPropertySetter>().SetDeletion(product);

        Assert.True(product.IsDeleted);
        Assert.Equal((s_t1, user, s_t1), (product.DeletedAt, product.DeletedBy, product.UpdatedAt));
        Assert.IsAssignableFrom<IFullAuditedObject>(product);
    }
}
