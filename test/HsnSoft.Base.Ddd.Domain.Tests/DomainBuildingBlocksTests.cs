using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;

namespace HsnSoft.Base.Ddd.Domain.Tests;

public class DomainBuildingBlocksTests
{
    private sealed class Product(string name, int price) : FullAuditedEntity<Guid>(Guid.NewGuid())
    {
        public string Name { get; private set; } = name;

        public int Price { get; private set; } = price;
    }

    private sealed class Plain(int id) : Entity<int>(id);

    private sealed class Empty : Entity<Guid>;

    private sealed class Recorder : ISortVisitor<Product>
    {
        public List<string> Clauses { get; } = [];

        public void Visit<TKey>(System.Linq.Expressions.Expression<Func<Product, TKey>> key, bool descending)
            => Clauses.Add($"{key.Body} {(descending ? "desc" : "asc")}");
    }

    [Fact]
    public void AnEntity_HasItsId_AndDefaultsToNone()
    {
        Guid id = Guid.NewGuid();

        Assert.Equal(7, new Plain(7).Id);
        Assert.Equal(Guid.Empty, new Empty().Id);
        Assert.Equal("[ENTITY: Plain] Id = 7", new Plain(7).ToString());
        Assert.NotEqual(id, new Product("a", 1).Id);
    }

    [Fact]
    public void TheAuditedEntities_KeepWhatThePersistenceMarks()
    {
        Product product = new("lamp", 10);
        Guid creator = Guid.NewGuid();
        Guid editor = Guid.NewGuid();
        Guid deleter = Guid.NewGuid();
        DateTimeOffset t1 = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

        product.MarkCreated(t1, creator);
        product.MarkModified(t1.AddHours(1), editor);

        Assert.Equal((t1, creator, t1.AddHours(1), editor), (product.CreatedAt, product.CreatedBy, product.UpdatedAt, product.UpdatedBy));
        Assert.False(product.IsDeleted);

        product.MarkDeleted(t1.AddHours(2), deleter);

        Assert.True(product.IsDeleted);
        Assert.Equal((t1.AddHours(2), deleter), (product.DeletedAt, product.DeletedBy));
    }

    private static SortFields<Product> Fields() => new SortFields<Product>().Add("name", product => product.Name).Add("Price", product => product.Price);

    [Fact]
    public void TheSortingText_IsParsedAgainstTheFieldsOffered_InOrder()
    {
        Recorder recorder = new();

        Fields().Parse("price DESC, name", "name asc").Accept(recorder);

        Assert.Equal(["product.Price desc", "product.Name asc"], recorder.Clauses);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankSorting_IsTheDefaultOne(string? text)
    {
        Recorder recorder = new();

        Fields().Parse(text, "price desc").Accept(recorder);

        Assert.Equal(["product.Price desc"], recorder.Clauses);
    }

    [Theory]
    [InlineData("passwordHash")]
    [InlineData("name asc, secret desc")]
    public void AFieldThatIsNotOffered_IsRefused_WithTheKeyAndTheFields(string text)
    {
        DomainException error = Assert.Throws<DomainException>(() => Fields().Parse(text, "name"));

        Assert.Equal("Validation:UnknownSortField", error.MessageKey);
        Assert.Contains("name", error.MessageArgs[1]!.ToString()!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("name up")]
    [InlineData("name asc extra")]
    public void ADirectionThatIsNotAscOrDesc_IsRefused(string text)
    {
        DomainException error = Assert.Throws<DomainException>(() => Fields().Parse(text, "name"));

        Assert.Equal("Validation:UnknownSortDirection", error.MessageKey);
    }

    [Fact]
    public void TheFieldsOffered_AreListed_AndCheckTheirInput()
    {
        SortFields<Product> fields = Fields();

        Assert.Equal(["name", "Price"], fields.Names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
        Assert.Throws<ArgumentException>(() => fields.Add(" ", product => product.Name));
        Assert.Throws<ArgumentNullException>(() => fields.Add<string>("x", null!));
    }

    [Fact]
    public void TheOptionsOfAList_DefaultToNothingNarrowed_AndAPageStartsAtTheFirst()
    {
        PagedQueryOptions<Product> paged = new();

        Assert.Null(paged.Filter);
        Assert.Null(paged.Sort);
        Assert.Null(paged.MaxResultCount);
        Assert.Equal(1, paged.PageNumber);
        Assert.Equal(10, PagedQueryOptions<Product>.DefaultPageSize);
        Assert.Equal(("a", 3), (new PagedQueryResult<string>(["a"], 3).Items[0], (int)new PagedQueryResult<string>(["a"], 3).TotalCount));
    }
}
