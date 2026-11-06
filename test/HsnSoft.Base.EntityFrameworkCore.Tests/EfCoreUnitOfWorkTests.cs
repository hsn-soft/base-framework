using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreUnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public EfCoreUnitOfWorkTests()
    {
        _connection.Open();
        using RunnerContext db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    public sealed class Counter(int id) : Entity<int>(id);

    public sealed class RunnerContext(DbContextOptions<RunnerContext> options) : DbContext(options)
    {
        public DbSet<Counter> Counters => Set<Counter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Counter>().Property(counter => counter.Id).ValueGeneratedNever();
    }

    private RunnerContext NewContext() => new(new DbContextOptionsBuilder<RunnerContext>().UseSqlite(_connection).Options);

    [Fact]
    public async Task WhatTheWorkSaved_IsCommittedTogether_AndTheResultIsReturned()
    {
        await using RunnerContext db = NewContext();
        EfCoreUnitOfWork<RunnerContext> runner = new(db);

        int result = await runner.ExecuteAsync(async token =>
        {
            db.Counters.Add(new Counter(1));
            await db.SaveChangesAsync(token);
            db.Counters.Add(new Counter(2));
            await db.SaveChangesAsync(token);
            return 42;
        });

        await using RunnerContext check = NewContext();
        Assert.Equal(42, result);
        Assert.Equal(2, await check.Counters.CountAsync());
    }

    [Fact]
    public async Task WhenTheWorkThrows_NothingOfItIsKept()
    {
        await using RunnerContext db = NewContext();
        EfCoreUnitOfWork<RunnerContext> runner = new(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync(async token =>
        {
            db.Counters.Add(new Counter(1));
            await db.SaveChangesAsync(token);
            throw new InvalidOperationException("stop");
        }));

        await using RunnerContext check = NewContext();
        Assert.Equal(0, await check.Counters.CountAsync());
    }

    [Fact]
    public async Task WhatTheContextTrackedBeforeTheWork_IsForgottenWhenItBegins()
    {
        await using RunnerContext db = NewContext();
        db.Counters.Add(new Counter(7));
        EfCoreUnitOfWork<RunnerContext> runner = new(db);

        await runner.ExecuteAsync(token =>
        {
            Assert.Empty(db.ChangeTracker.Entries());
            return Task.CompletedTask;
        });

        await using RunnerContext check = NewContext();
        Assert.Equal(0, await check.Counters.CountAsync());
    }

    [Fact]
    public async Task TheWork_MustBeGiven()
    {
        await using RunnerContext db = NewContext();
        EfCoreUnitOfWork<RunnerContext> runner = new(db);

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.ExecuteAsync((Func<CancellationToken, Task>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.ExecuteAsync((Func<CancellationToken, Task<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => EfCoreUnitOfWorkServiceCollectionExtensions.AddEfCoreUnitOfWork<RunnerContext>(null!));
    }

    [Fact]
    public async Task ItIsRegisteredAsTheTransactionOfTheService()
    {
        ServiceCollection services = new();
        services.AddDbContext<RunnerContext>(options => options.UseSqlite(_connection));
        services.AddEfCoreUnitOfWork<RunnerContext>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Assert.IsType<EfCoreUnitOfWork<RunnerContext>>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }
}
