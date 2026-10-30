using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.IntegrationTests.Data;

[Collection(Collections.MySql)]
public sealed class MySqlRepositoryTests(MySqlFixture mySql) : IAsyncLifetime
{
    private string _connection = null!;
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        // once: the options of the context are built for every scope, and every scope has to reach the same database
        _connection = mySql.NewConnectionString();
        _host = await Support.StartHostAsync(services => services
            .AddBaseEntityFramework()
            .AddDbContext<PartContext>((provider, options) => options.UseMySQL(_connection + ";Connection Timeout=60;Default Command Timeout=60").UseBaseEntityConcepts(provider))
            .AddEfCoreRepository<PartContext, Part, Guid>());
        await _host.Services.EnsureDatabaseCreatedAsync<PartContext>();
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
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
