using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreProjectionTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly List<string> _sql = [];
    private ServiceProvider _provider = null!;

    public sealed class Writer(string name) : Entity<Guid>(Guid.NewGuid())
    {
        public string Name { get; set; } = name;

        public string Country { get; set; } = "unknown";
    }

    public sealed class Book : Entity<Guid>
    {
        // for EF Core: a navigation cannot be bound to a constructor parameter
        private Book()
            : base(Guid.NewGuid())
        {
        }

        public Book(string title, Writer writer)
            : base(Guid.NewGuid())
        {
            Title = title;
            Writer = writer;
        }

        public string Title { get; set; } = string.Empty;

        public int Pages { get; set; } = 100;

        public Writer Writer { get; set; } = null!;
    }

    public sealed class BookDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
    }

    public sealed class BookWithWriterDto
    {
        public string Title { get; set; } = string.Empty;

        public string WriterName { get; set; } = string.Empty;
    }

    public sealed class ProjectionContext(DbContextOptions<ProjectionContext> options) : DbContext(options)
    {
        public DbSet<Writer> Writers => Set<Writer>();

        public DbSet<Book> Books => Set<Book>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Writer>().Property(writer => writer.Id).ValueGeneratedNever();
            modelBuilder.Entity<Book>().Property(book => book.Id).ValueGeneratedNever();
        }
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<ProjectionContext>(options => options.UseSqlite(_connection).AddInterceptors(new SqlCapture(_sql)));
        services.AddEfCoreRepository<ProjectionContext, Book, Guid>();
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        ProjectionContext db = scope.ServiceProvider.GetRequiredService<ProjectionContext>();
        await db.Database.EnsureCreatedAsync();
        Writer ada = new("Ada") { Country = "UK" };
        db.Books.AddRange(new Book("Notes", ada) { Pages = 30 }, new Book("Engines", ada) { Pages = 90 }, new Book("Zeta", new Writer("Zed")));
        await db.SaveChangesAsync();
    }

    public void Dispose() => _connection.Dispose();

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class SqlCapture(List<string> sql) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            sql.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private static readonly SortFields<Book> s_sort = new SortFields<Book>().Add("title", book => book.Title);

    private static IEfCoreRepository<Book, Guid> Books(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IEfCoreRepository<Book, Guid>>();

    private async Task<Guid> IdOfAsync(string title)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return (await Books(scope).FindAsync(book => book.Title == title))!.Id;
    }

    [Fact]
    public async Task ADtoReadsOnlyItsColumns_AndJoinsNothing()
    {
        Guid id = await IdOfAsync("Notes");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        _sql.Clear();

        BookDto? dto = await Books(scope).FindAsync<BookDto>(id);

        Assert.Equal("Notes", dto?.Title);
        string sql = Assert.Single(_sql);
        Assert.DoesNotContain("Pages", sql);
        Assert.DoesNotContain("JOIN", sql);
        Assert.Null(await Books(scope).FindAsync<BookDto>(Guid.NewGuid()));
    }

    [Fact]
    public async Task ADtoThatReachesAnObject_JoinsOnlyThen()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        _sql.Clear();

        IReadOnlyList<BookWithWriterDto> list = await Books(scope).GetListAsync<BookWithWriterDto>(new ListQueryOptions<Book> { Sort = s_sort.Parse("title", "title") });

        Assert.Equal([("Engines", "Ada"), ("Notes", "Ada"), ("Zeta", "Zed")], list.Select(book => (book.Title, book.WriterName)));
        string sql = Assert.Single(_sql);
        Assert.Contains("JOIN", sql);
        Assert.DoesNotContain("Country", sql);
    }

    [Fact]
    public async Task APageOfDtos_HasTheTotalOfTheFilter()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        PagedQueryResult<BookDto> page = await Books(scope).GetPagedListAsync<BookDto>(new PagedQueryOptions<Book> { Filter = book => book.Pages > 50, Sort = s_sort.Parse("title", "title"), MaxResultCount = 1 });

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(["Engines"], page.Items.Select(book => book.Title));
    }

    [Fact]
    public async Task AGivenSelector_IsUsedInsteadOfTheGeneratedOne()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        IReadOnlyList<BookDto> list = await Books(scope).GetListAsync<BookDto>(new ListQueryOptions<Book> { Sort = s_sort.Parse("title", "title") }, book => new BookDto { Id = book.Id, Title = "u" + book.Title });
        BookDto? one = await Books(scope).FindAsync(await IdOfAsync("Notes"), (System.Linq.Expressions.Expression<Func<Book, BookDto>>)(book => new BookDto { Title = "x" + book.Title }));
        PagedQueryResult<BookDto> page = await Books(scope).GetPagedListAsync<BookDto>(new PagedQueryOptions<Book> { Sort = s_sort.Parse("title", "title"), MaxResultCount = 1 }, book => new BookDto { Title = "p" + book.Title });

        Assert.Equal(["uEngines", "uNotes", "uZeta"], list.Select(book => book.Title));
        Assert.Equal("xNotes", one?.Title);
        Assert.Equal(["pEngines"], page.Items.Select(book => book.Title));
    }

    [Fact]
    public async Task AnEntityHasNothingJoined_UnlessAnIncludeSaysSo()
    {
        Guid id = await IdOfAsync("Notes");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        _sql.Clear();

        Book plain = (await Books(scope).GetListAsync(new ListQueryOptions<Book> { Filter = book => book.Id == id })).Single();

        Assert.DoesNotContain("JOIN", Assert.Single(_sql));
        Assert.Null(plain.Writer);
        _sql.Clear();

        IReadOnlyList<Book> included = await Books(scope).GetListAsync(new ListQueryOptions<Book> { Filter = book => book.Id == id }, query => query.Include(book => book.Writer));
        Book found = (await Books(scope).FindAsync(id, query => query.Include(book => book.Writer)))!;
        PagedQueryResult<Book> page = await Books(scope).GetPagedListAsync(new PagedQueryOptions<Book> { Filter = book => book.Id == id }, query => query.Include(book => book.Writer));

        // the count of a page is made without the join: it does not change how many match
        Assert.All(_sql.Where(sql => !sql.Contains("COUNT(*)")), sql => Assert.Contains("JOIN", sql));
        Assert.Equal("Ada", included.Single().Writer.Name);
        Assert.Equal("Ada", found.Writer.Name);
        Assert.Equal("Ada", page.Items.Single().Writer.Name);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task AnIncludeIsRequired_WhenTheOverloadWithOneIsCalled()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        await Assert.ThrowsAsync<ArgumentNullException>(() => Books(scope).FindAsync(Guid.NewGuid(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Books(scope).GetListAsync(new ListQueryOptions<Book>(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Books(scope).GetPagedListAsync(new PagedQueryOptions<Book>(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Books(scope).GetListAsync<BookDto>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Books(scope).GetPagedListAsync<BookDto>(null!));
    }
}
