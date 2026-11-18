using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class Person(string name, int age) : FullAuditedEntity<Guid>(Guid.NewGuid())
{
    public string Name { get; set; } = name;

    public int Age { get; set; } = age;
}

public sealed class Handle(Guid id, string name) : Entity<Guid>(id)
{
    public string Name { get; set; } = name;
}

public sealed class PersonContext(DbContextOptions<PersonContext> options, HsnSoft.Base.Data.IDataFilter? dataFilter = null) : BaseDbContext(options, dataFilter)
{
    public DbSet<Person> People => Set<Person>();

    public DbSet<Handle> Handles => Set<Handle>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Handle>().Property(handle => handle.Id).ValueGeneratedNever();
        modelBuilder.Entity<Handle>().Property(handle => handle.Name).HasMaxLength(100);
        modelBuilder.Entity<Handle>().HasIndex(handle => handle.Name).IsUnique().HasDatabaseName("ux_handle_name");
        modelBuilder.Entity<Person>().Property(person => person.Id).ValueGeneratedNever();
    }
}

/// <summary>
/// What the repository does for every scenario of a store, against each real database (the scenarios of the old framework's repository tests, which ran on PostgreSQL, written again for the new contract): nothing in an empty store, an entity that comes back as it
/// went in, duplicates, lists and pages with a filter and a sort, many entities at once, soft delete, and an entity that is not there to be updated or deleted.
/// </summary>
public abstract class RepositoryScenarios : IAsyncLifetime
{
    private static readonly SortFields<Person> s_sort = new SortFields<Person>().Add("name", person => person.Name).Add("age", person => person.Age);

    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await Support.StartHostAsync(services => services
            .AddSingleton<ICurrentUser>(new Caller())
            .AddBaseEntityFramework()
            .AddDbContext<PersonContext>((provider, options) => UseProvider(options).UseBaseEntityConcepts(provider))
            .AddEfCoreRepository<PersonContext, Person, Guid>()
            .AddEfCoreRepository<PersonContext, Handle, Guid>());
        await _host.Services.EnsureDatabaseCreatedAsync<PersonContext>();
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Points the options at the database of the fixture.</summary>
    protected abstract DbContextOptionsBuilder UseProvider(DbContextOptionsBuilder options);

    private sealed class Caller : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id { get; } = Guid.NewGuid();

        public string? UserName => "scenario";
    }

    private AsyncServiceScope Scope() => _host.Services.CreateAsyncScope();

    private static IRepository<Person, Guid> People(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Person, Guid>>();

    // each scenario names its people with a prefix of its own, so the scenarios of one database do not meet
    private async Task<List<Person>> SeedAsync(string prefix, int count, Func<int, int>? age = null)
    {
        List<Person> made = [.. Enumerable.Range(1, count).Select(number => new Person($"{prefix}-{number:00}", age?.Invoke(number) ?? number))];
        await using AsyncServiceScope scope = Scope();
        await People(scope).AddManyAsync(made);
        return made;
    }

    [Fact]
    public async Task InAStoreWithNothingInIt_EveryReadAnswersNothing()
    {
        const string prefix = "empty";
        await using AsyncServiceScope scope = Scope();
        IRepository<Person, Guid> people = People(scope);

        Assert.Null(await people.FindAsync(Guid.NewGuid()));
        Assert.Null(await people.FindAsync(person => person.Name == $"{prefix}-1"));
        Assert.Empty(await people.GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name.StartsWith(prefix) }));
        PagedQueryResult<Person> page = await people.GetPagedListAsync(new PagedQueryOptions<Person> { Filter = person => person.Name.StartsWith(prefix) });
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, await people.CountAsync(person => person.Name.StartsWith(prefix)));
        Assert.False(await people.AnyAsync(person => person.Name.StartsWith(prefix)));
    }

    [Fact]
    public async Task AnEntity_ComesBackAsItWentIn_WithItsAuditFieldsToTheMillisecond()
    {
        Person added = new("roundtrip", 41);
        await using (AsyncServiceScope scope = Scope())
        {
            await People(scope).AddAsync(added);
        }

        await using AsyncServiceScope read = Scope();
        Person found = (await People(read).FindAsync(added.Id))!;
        Person byPredicate = (await People(read).FindAsync(person => person.Name == "roundtrip"))!;

        Assert.Equal((added.Id, "roundtrip", 41, added.CreatedBy), (found.Id, found.Name, found.Age, found.CreatedBy));
        Assert.NotNull(added.CreatedBy);
        Assert.InRange((found.CreatedAt - added.CreatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
        Assert.Equal(added.Id, byPredicate.Id);
        Assert.False(found.IsDeleted);
    }

    [Fact]
    public async Task EntitiesWithTheSameName_AreNotAnError_AndASortedLimitOfOneIsTheFirstOfThem()
    {
        await SeedAsync("same", 0);
        await using (AsyncServiceScope seed = Scope())
        {
            await People(seed).AddManyAsync([new Person("same", 18), new Person("same", 61), new Person("same", 35)]);
        }

        await using AsyncServiceScope scope = Scope();
        Person? any = await People(scope).FindAsync(person => person.Name == "same");
        IReadOnlyList<Person> oldest = await People(scope).GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name == "same", Sort = s_sort.Parse("age desc", "age"), MaxResultCount = 1 });
        IReadOnlyList<Person> youngest = await People(scope).GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name == "same", Sort = s_sort.Parse("age", "age"), MaxResultCount = 1 });

        Assert.NotNull(any);
        Assert.Equal(61, Assert.Single(oldest).Age);
        Assert.Equal(18, Assert.Single(youngest).Age);
    }

    [Fact]
    public async Task ListsAndPages_FilterSortAndCount_AlsoWithAFilterPutTogetherFromParts()
    {
        await SeedAsync("list", 25);
        await using AsyncServiceScope scope = Scope();
        IRepository<Person, Guid> people = People(scope);

        PagedQueryResult<Person> second = await people.GetPagedListAsync(new PagedQueryOptions<Person> { Filter = person => person.Name.StartsWith("list-"), Sort = s_sort.Parse("name desc", "name"), PageNumber = 2, MaxResultCount = 10 });
        System.Linq.Expressions.Expression<Func<Person, bool>>? either = FilterBuilder.AnyOf<Person>(person => person.Name == "list-03", null, person => person.Age >= 24);
        System.Linq.Expressions.Expression<Func<Person, bool>> built = new FilterBuilder<Person>().And(person => person.Name.StartsWith("list-")).And(either).Build();
        IReadOnlyList<Person> chosen = await people.GetListAsync(new ListQueryOptions<Person> { Filter = built, Sort = s_sort.Parse("age", "age") });
        System.Linq.Expressions.Expression<Func<Person, bool>> ored = new FilterBuilder<Person>().And(person => person.Name == "list-01").Or(person => person.Name == "list-02").Build();

        Assert.Equal(25, second.TotalCount);
        Assert.Equal(Enumerable.Range(6, 10).Reverse().Select(number => $"list-{number:00}"), second.Items.Select(person => person.Name));
        Assert.Equal(["list-03", "list-24", "list-25"], chosen.Select(person => person.Name));
        Assert.Equal(2, await people.CountAsync(ored));
        Assert.Equal(25, await people.CountAsync(person => person.Name.StartsWith("list-")));
        Assert.True(await people.AnyAsync(person => person.Name == "list-25"));
        Assert.False(await people.AnyAsync(person => person.Name == "list-26"));
    }

    [Fact]
    public async Task ManyEntities_AreAdded_Updated_AndDeleted_InOneGo()
    {
        List<Person> seeded = await SeedAsync("many", 5);
        await using (AsyncServiceScope scope = Scope())
        {
            List<Person> loaded = [.. await People(scope).GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name.StartsWith("many-") })];
            Assert.Equal(5, loaded.Count);
            foreach (Person person in loaded)
            {
                person.Age += 100;
            }

            await People(scope).UpdateManyAsync(loaded);
        }

        await using (AsyncServiceScope scope = Scope())
        {
            Assert.All(await People(scope).GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name.StartsWith("many-") }), person => Assert.True(person.Age > 100));
            List<Person> toDelete = [.. await People(scope).GetListAsync(new ListQueryOptions<Person> { Filter = person => person.Name.StartsWith("many-") && person.Age > 102 })];
            await People(scope).DeleteManyAsync(toDelete);
            Assert.Equal(2, await People(scope).CountAsync(person => person.Name.StartsWith("many-")));
        }

        Assert.Equal(5, seeded.Count);
    }

    [Fact]
    public async Task ASoftDeletedEntity_IsHidden_ButStaysInTheStore()
    {
        Person person = (await SeedAsync("soft", 1))[0];
        await using (AsyncServiceScope scope = Scope())
        {
            await People(scope).DeleteAsync((await People(scope).FindAsync(person.Id))!);
        }

        await using AsyncServiceScope read = Scope();
        Assert.Null(await People(read).FindAsync(person.Id));
        Assert.False(await People(read).AnyAsync(candidate => candidate.Id == person.Id));
        Assert.True(await People(read).AnyAsync(candidate => candidate.Id == person.Id, ignoreFilters: true));
        Assert.Equal(0, await People(read).CountAsync(candidate => candidate.Name == "soft-01"));
    }

    [Fact]
    public async Task AnEntityThatIsNotThere_IsA404_NotFound_ToUpdateAndToDelete_OneAtATimeAndInBulk()
    {
        Person tracked = (await SeedAsync("gone", 1))[0];
        await using (AsyncServiceScope remover = Scope())
        {
            await People(remover).DeleteWhereAsync(person => person.Id == tracked.Id);
            await People(remover).DeleteWhereAsync(person => person.Name == "gone-01");
        }

        // removed for good (a row that is not there at all): an entity that was never saved stands for it
        Person neverSaved = new("never", 1);
        await using AsyncServiceScope scope = Scope();
        IRepository<Person, Guid> people = People(scope);

        foreach (Func<Task> call in new Func<Task>[]
        {
            () => people.UpdateAsync(neverSaved),
            () => people.DeleteAsync(neverSaved),
            () => people.UpdateManyAsync([neverSaved]),
            () => people.DeleteManyAsync([neverSaved]),
        })
        {
            RepositoryException gone = await Assert.ThrowsAsync<RepositoryException>(call);
            Assert.Equal((404, RepositoryFailure.NotFound, nameof(Person)), (gone.HttpStatusCode, gone.Failure, gone.EntityName));
            Assert.Equal(neverSaved.Id.ToString(), Assert.Single(gone.Ids));
        }
    }

    private static IRepository<Handle, Guid> Handles(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Handle, Guid>>();

    [Fact]
    public async Task AValueThatAnotherRecordHas_IsADuplicateKey_409_NamingTheIndex_OnEveryProvider()
    {
        await using (AsyncServiceScope seed = Scope())
        {
            await Handles(seed).AddAsync(new Handle(Guid.NewGuid(), "dup-one"));
        }

        // a scope for each call: what a write that failed left in the change tracker is not for the next one to save (a request has a scope of its own)
        await using AsyncServiceScope first = Scope();
        RepositoryException add = await Assert.ThrowsAsync<RepositoryException>(() => Handles(first).AddAsync(new Handle(Guid.NewGuid(), "dup-one")));
        await using AsyncServiceScope second = Scope();
        RepositoryException many = await Assert.ThrowsAsync<RepositoryException>(() => Handles(second).AddManyAsync([new Handle(Guid.NewGuid(), "dup-two"), new Handle(Guid.NewGuid(), "dup-one")]));
        await using AsyncServiceScope third = Scope();
        Handle other = await Handles(third).AddAsync(new Handle(Guid.NewGuid(), "dup-three"));
        other.Name = "dup-one";
        RepositoryException update = await Assert.ThrowsAsync<RepositoryException>(() => Handles(third).UpdateAsync(other));

        foreach (RepositoryException failed in new[] { add, many, update })
        {
            Assert.Equal((RepositoryFailure.DuplicateKey, 409, RepositoryException.DuplicateCode, nameof(Handle)), (failed.Failure, failed.HttpStatusCode, failed.ErrorCode, failed.EntityName));
            Assert.Contains("ux_handle_name", failed.Constraint, StringComparison.OrdinalIgnoreCase);   // the name of the index, as each database says it
            Assert.DoesNotContain("dup-one", failed.Message, StringComparison.Ordinal);                // the value is not told
        }

        await using AsyncServiceScope check = Scope();
        Assert.Equal(0, await Handles(check).CountAsync(handle => handle.Name == "dup-two"));   // the batch was rolled back
    }

    [Fact]
    public async Task AddAsync_GivesBackTheSameInstance_AndAKeyForAnEntityThatHasNone()
    {
        Person person = new("same-instance", 30);
        await using AsyncServiceScope scope = Scope();

        Person added = await People(scope).AddAsync(person);

        Assert.Same(person, added);
        Assert.NotNull(added.CreatedBy);
        Assert.NotEqual(default, added.CreatedAt);
    }

    [Fact]
    public async Task AddManyAsync_IsAllOrNone_InOneTransaction()
    {
        await using AsyncServiceScope scope = Scope();
        List<Person> batch = [new("all-or-none-1", 1), new("all-or-none-2", 2), new(null!, 3)];   // the third has no name: the database refuses the row

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => People(scope).AddManyAsync(batch));

        await using AsyncServiceScope check = Scope();
        Assert.Equal(0, await People(check).CountAsync(person => person.Name.StartsWith("all-or-none")));   // the two good rows are not there either
    }

    [Fact]
    public async Task UpdateAsync_AndUpdateManyAsync_SayWhatChanged_AndWriteNothingForWhatDidNot()
    {
        List<Person> seeded = await SeedAsync("outcome", 3);
        await using AsyncServiceScope scope = Scope();
        Person one = (await People(scope).FindAsync(seeded[0].Id))!;
        Person two = (await People(scope).FindAsync(seeded[1].Id))!;
        Person three = (await People(scope).FindAsync(seeded[2].Id))!;

        Assert.Equal(WriteOutcome.NotModified, await People(scope).UpdateAsync(one));
        one.Age = 99;
        Assert.Equal(WriteOutcome.Modified, await People(scope).UpdateAsync(one));
        two.Age = 98;
        WriteResult result = await People(scope).UpdateManyAsync([one, two, three]);   // one was just written, two changed, three did not

        Assert.Equal((3, 1, 2), (result.Requested, result.Modified, result.NotModified));
        await using AsyncServiceScope check = Scope();
        Assert.Equal((99, 98, 3), ((await People(check).FindAsync(seeded[0].Id))!.Age, (await People(check).FindAsync(seeded[1].Id))!.Age, (await People(check).FindAsync(seeded[2].Id))!.Age));
    }

    [Fact]
    public async Task UpdateManyAsync_WithOneRecordGone_FailsAll_NamesIt_AndWritesNone()
    {
        List<Person> seeded = await SeedAsync("many-gone", 3);
        await using AsyncServiceScope scope = Scope();
        List<Person> found = [];
        foreach (Person person in seeded)
        {
            found.Add((await People(scope).FindAsync(person.Id))!);
        }

        await using (AsyncServiceScope other = Scope())
        {
            // removed for good (a soft delete leaves the row, and an entity with no stamp cannot tell): the row is not there at all
            await other.ServiceProvider.GetRequiredService<PersonContext>().People.IgnoreQueryFilters().Where(person => person.Id == seeded[1].Id).ExecuteDeleteAsync();
        }

        found.ForEach(person => person.Age = 77);
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => People(scope).UpdateManyAsync(found));

        Assert.Equal((RepositoryFailure.NotFound, 404), (failed.Failure, failed.HttpStatusCode));
        Assert.Equal(seeded[1].Id.ToString(), Assert.Single(failed.Ids));
        await using AsyncServiceScope check = Scope();
        Assert.Equal(1, (await People(check).FindAsync(seeded[0].Id))!.Age);   // nothing was written: one transaction, rolled back
        Assert.Equal(3, (await People(check).FindAsync(seeded[2].Id))!.Age);
    }

    [Fact]
    public async Task DeleteWhere_SaysHowManyItMarked()
    {
        await SeedAsync("where", 6);
        await using AsyncServiceScope scope = Scope();

        long marked = await People(scope).DeleteWhereAsync(person => person.Name.StartsWith("where-") && person.Age <= 4);

        Assert.Equal(4, marked);
        Assert.Equal(2, await People(scope).CountAsync(person => person.Name.StartsWith("where-")));
        Assert.Equal(0, await People(scope).DeleteWhereAsync(person => person.Name == "where-none"));
    }
}

[Collection(Collections.Postgres)]
public sealed class PostgreSqlRepositoryScenarios(PostgresFixture postgres) : RepositoryScenarios
{
    // once for the scenario: the options are built for every scope, and every scope has to reach the same database
    private readonly string _connection = postgres.NewConnectionString() + ";Timeout=60;Command Timeout=60";

    protected override DbContextOptionsBuilder UseProvider(DbContextOptionsBuilder options) => options.UseNpgsql(_connection);
}

[Collection(Collections.MySql)]
public sealed class MySqlRepositoryScenarios(MySqlFixture mySql) : RepositoryScenarios
{
    private readonly string _connection = mySql.NewConnectionString() + ";Connection Timeout=60;Default Command Timeout=60";

    protected override DbContextOptionsBuilder UseProvider(DbContextOptionsBuilder options) => options.UseMySQL(_connection);
}

[Collection(Collections.SqlServer)]
public sealed class SqlServerRepositoryScenarios(SqlServerFixture sqlServer) : RepositoryScenarios
{
    private readonly string _connection = sqlServer.NewConnectionString();

    protected override DbContextOptionsBuilder UseProvider(DbContextOptionsBuilder options) => options.UseSqlServer(_connection);
}
