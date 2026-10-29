using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class Part(Guid id, string sku, int stock) : Entity<Guid>(id)
{
    public string Sku { get; set; } = sku;

    public int Stock { get; set; } = stock;
}

public sealed class PartContext(DbContextOptions<PartContext> options) : DbContext(options)
{
    public DbSet<Part> Parts => Set<Part>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Part>().Property(part => part.Id).ValueGeneratedNever();
    }
}

public sealed class PostgreSqlRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _host = await Support.StartHostAsync(services => services
            .AddBaseEntityFramework()
            .AddDbContext<PartContext>((provider, options) => options.UseNpgsql(_container.GetConnectionString() + ";Timeout=60;Command Timeout=60").UseBaseEntityConcepts(provider))
            .AddEfCoreRepository<PartContext, Part, Guid>());
        await _host.Services.EnsureDatabaseCreatedAsync<PartContext>();
    }

    public async Task DisposeAsync()
    {
        _host.Dispose();
        await Support.DisposeAsync(_container);
    }

    private AsyncServiceScope Scope() => _host.Services.CreateAsyncScope();

    [Fact]
    public async Task UpdateWhere_ChangesACounterOnlyWhileTheConditionHolds_WhateverTheNumberOfCallersAtOnce()
    {
        Part part = new(Guid.NewGuid(), "lamp", 10);
        await using (AsyncServiceScope seed = Scope())
        {
            await seed.ServiceProvider.GetRequiredService<IRepository<Part, Guid>>().AddAsync(part);
        }

        // twenty callers, each wants one of the ten that are there: the test and the change are one statement, so exactly ten get one
        int[] changed = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using AsyncServiceScope scope = Scope();
            return await scope.ServiceProvider.GetRequiredService<IEfCoreRepository<Part, Guid>>()
                .UpdateWhereAsync(candidate => candidate.Id == part.Id && candidate.Stock >= 1, set => set.SetProperty(candidate => candidate.Stock, candidate => candidate.Stock - 1));
        }));

        await using AsyncServiceScope read = Scope();
        Assert.Equal(10, changed.Count(count => count == 1));
        Assert.Equal(10, changed.Count(count => count == 0));
        Assert.Equal(0, (await read.ServiceProvider.GetRequiredService<IRepository<Part, Guid>>().FindAsync(part.Id))!.Stock);
    }
}
