using FluentAssertions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Test.Unit.Fixtures;
using HsnSoft.Base.Test.Unit.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Test.Unit.Utils;

namespace HsnSoft.Base.Test.Unit.Repositories;

public class MongoGenericRepositoryUnitTests(MongoFixture fixture) : IClassFixture<MongoFixture>
{
    private readonly string _connectionString = fixture.Runner.ConnectionString;

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturn_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.InsertOneAsync(expected);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetByIdAsync(expected.Id);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.GetByIdAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion GetByIdAsync

    #region GetByIdOrDefaultAsync

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturn_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.InsertOneAsync(expected);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetByIdOrDefaultAsync(expected.Id);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);
    }

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturnNull_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetByIdOrDefaultAsync(Guid.NewGuid());

        // Assert
        actual.Should().BeNull();
    }

    #endregion GetByIdOrDefaultAsync

    #region GetSingleAsync

    [Fact]
    public async Task GetSingleAsync_ShouldReturn_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.InsertOneAsync(expected);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetSingleAsync(x => x.Id == expected.Id);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Id == expected.Id)
            .Build();

        // Act
        actual = await repo.GetSingleAsync(filter);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);
    }

    [Fact]
    public async Task GetSingleAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.GetSingleAsync(x => x.Id == Guid.NewGuid()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetSingleAsync_ShouldThrow_WhenEntityDuplicate()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "Tester", 61), new(Guid.NewGuid(), "Tester", 61) }.AsReadOnly()
        );
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.GetSingleAsync(x => x.Name == "Tester"))
            .Should()
            .ThrowAsync<EntityDuplicateException>();
    }

    #endregion GetSingleAsync

    #region GetSingleOrDefaultAsync

    [Fact]
    public async Task GetSingleOrDefaultAsync_ShouldReturn_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.InsertOneAsync(expected);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetSingleOrDefaultAsync(x => x.Id == expected.Id);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Id == expected.Id)
            .Build();

        // Act
        actual = await repo.GetSingleOrDefaultAsync(filter);

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);
    }

    [Fact]
    public async Task GetSingleOrDefaultAsync_ShouldReturnNull_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetSingleOrDefaultAsync(e => e.Id == Guid.NewGuid());

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetSingleOrDefaultAsync_ShouldThrow_WhenEntityDuplicate()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "Tester", 61), new(Guid.NewGuid(), "Tester", 61) }.AsReadOnly()
        );

        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.GetSingleOrDefaultAsync(x => x.Name == "Tester"))
            .Should()
            .ThrowAsync<EntityDuplicateException>();
    }

    #endregion GetSingleOrDefaultAsync

    #region GetFirstOrDefaultAsync

    [Fact]
    public async Task GetFirstOrDefaultAsync_ShouldReturnOrderFirst_WhenEntityDuplicateExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "Tester", 18), expected }.AsReadOnly());
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetFirstOrDefaultAsync(
            x => x.Name == "Tester",
            o => o.OrderByDescending(e => e.Age)
        );

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name == "Tester")
            .Build();

        // Act
        actual = await repo.GetFirstOrDefaultAsync(
            filter,
            o => o.OrderByDescending(e => e.Age)
        );

        // Assert
        actual.Should().NotBeNull();
        actual.ShouldBeEquivalentToWithMilliseconds(expected);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_ShouldReturnNull_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetFirstOrDefaultAsync(e => e.Id == Guid.NewGuid());

        // Assert
        actual.Should().BeNull();
    }

    #endregion GetFirstOrDefaultAsync

    #region GetListAsync

    [Fact]
    public async Task GetListAsync_ShouldReturnEmptyList_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetListAsync(new ListQueryOptions<TestEntity>());

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnList_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        await context.TestEntities.InsertManyAsync(expectedList);
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 21), new(Guid.NewGuid(), "TesterC", 22), new(Guid.NewGuid(), "TesterD", 23) });
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var noFilterList = await repo.GetListAsync(new ListQueryOptions<TestEntity>());

        // Assert
        noFilterList.Should().NotBeNull();
        noFilterList.Should().HaveCount(5);

        // Act
        var limitedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { ListLength = 3 });

        // Assert
        limitedList.Should().NotBeNull();
        limitedList.Should().HaveCount(3);

        // Act
        var filterList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { Filter = u => u.Name.Equals("TesterA") });

        // Assert
        filterList.Should().NotBeNull();
        filterList.Should().HaveCount(expectedList.Count);
        filterList.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        filterList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterList.Should().NotBeNull();
        filterList.Should().HaveCount(expectedList.Count);
        filterList.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Act
        var dynamicOrderedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { OrderByDynamic = $"{nameof(TestEntity.Name)} asc, {nameof(TestEntity.Age)} desc", ListLength = 1 });

        // Assert
        dynamicOrderedList.Should().NotBeNull();
        dynamicOrderedList.Should().HaveCount(1);
        dynamicOrderedList[0].Name.Should().Be("TesterA");
        dynamicOrderedList[0].Age.Should().Be(12);

        // Act
        var orderedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { OrderByEntity = o => o.OrderBy(e => e.Name).ThenByDescending(a => a.Age), ListLength = 1 });

        // Assert
        orderedList.Should().NotBeNull();
        orderedList.Should().HaveCount(1);
        orderedList[0].Name.Should().Be("TesterA");
        orderedList[0].Age.Should().Be(12);
    }

    #endregion GetListAsync

    #region GetPageListAsync

    [Fact]
    public async Task GetPageListAsync_ShouldReturnEmptyList_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetPageListAsync(new PageQueryOptions<TestEntity>());

        // Assert
        actual.Should().NotBeNull();
        actual.Items.Should().BeEmpty();
        actual.TotalCount.Should().Be(0);
        actual.PageNumber.Should().Be(1);
        actual.PageSize.Should().Be(new PageQueryOptions<TestEntity>().PageSize);
    }

    [Fact]
    public async Task GetPageListAsync_ShouldReturnList_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        await context.TestEntities.InsertManyAsync(expectedList);
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 21), new(Guid.NewGuid(), "TesterC", 22), new(Guid.NewGuid(), "TesterD", 23) });
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var noFilterPageList = await repo.GetPageListAsync(new PageQueryOptions<TestEntity>());

        // Assert
        noFilterPageList.Should().NotBeNull();
        noFilterPageList.Items.Should().HaveCount(5);
        noFilterPageList.TotalCount.Should().Be(5);
        noFilterPageList.PageNumber.Should().Be(1);
        noFilterPageList.PageSize.Should().Be(new PageQueryOptions<TestEntity>().PageSize);


        // Act
        var limitedPageList = await repo.GetPageListAsync(options: new PageQueryOptions<TestEntity> { PageNumber = 3, PageSize = 2 });

        // Assert
        limitedPageList.Should().NotBeNull();
        limitedPageList.Items.Should().HaveCount(1);
        limitedPageList.TotalCount.Should().Be(5);
        limitedPageList.PageNumber.Should().Be(3);
        limitedPageList.PageSize.Should().Be(2);

        // Act
        var filterPageList = await repo.GetPageListAsync(options: new PageQueryOptions<TestEntity> { Filter = u => u.Name.Equals("TesterA") });

        // Assert
        filterPageList.Should().NotBeNull();
        filterPageList.Items.Should().HaveCount(2);
        filterPageList.TotalCount.Should().Be(2);
        filterPageList.PageNumber.Should().Be(1);
        filterPageList.PageSize.Should().Be(new PageQueryOptions<TestEntity>().PageSize);
        filterPageList.Items.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        filterPageList = await repo.GetPageListAsync(options: new PageQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterPageList.Should().NotBeNull();
        filterPageList.Items.Should().HaveCount(2);
        filterPageList.TotalCount.Should().Be(2);
        filterPageList.PageNumber.Should().Be(1);
        filterPageList.PageSize.Should().Be(new PageQueryOptions<TestEntity>().PageSize);
        filterPageList.Items.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Act
        var dynamicOrderedPageList = await repo.GetPageListAsync(options: new PageQueryOptions<TestEntity> { OrderByDynamic = $"{nameof(TestEntity.Name)} asc, {nameof(TestEntity.Age)} desc", PageSize = 1 });

        // Assert
        dynamicOrderedPageList.Should().NotBeNull();
        dynamicOrderedPageList.Items.Should().HaveCount(1);
        dynamicOrderedPageList.Items[0].Name.Should().Be("TesterA");
        dynamicOrderedPageList.Items[0].Age.Should().Be(12);
        dynamicOrderedPageList.TotalCount.Should().Be(5);
        dynamicOrderedPageList.PageNumber.Should().Be(1);
        dynamicOrderedPageList.PageSize.Should().Be(1);

        // Act
        var orderedPageList = await repo.GetPageListAsync(options: new PageQueryOptions<TestEntity> { OrderByEntity = o => o.OrderBy(e => e.Name).ThenByDescending(a => a.Age), PageSize = 1 });

        // Assert
        orderedPageList.Should().NotBeNull();
        orderedPageList.Items.Should().HaveCount(1);
        orderedPageList.Items[0].Name.Should().Be("TesterA");
        orderedPageList.Items[0].Age.Should().Be(12);
        orderedPageList.TotalCount.Should().Be(5);
        orderedPageList.PageNumber.Should().Be(1);
        orderedPageList.PageSize.Should().Be(1);
    }

    #endregion GetPageListAsync

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_ShouldReturnZero_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        long? actual = await repo.GetCountAsync();

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(0);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCount_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 34), new(Guid.NewGuid(), "TesterA", 61) };
        await context.TestEntities.InsertManyAsync(expectedList);
        await context.TestEntities.InsertManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 1), new(Guid.NewGuid(), "TesterC", 2), new(Guid.NewGuid(), "TesterD", 3) });
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        long? noFilterListCount = await repo.GetCountAsync();

        // Assert
        noFilterListCount.Should().NotBeNull();
        noFilterListCount.Should().Be(5);

        // Act
        long? filterListCount = await repo.GetCountAsync(filter: u => u.Name.Equals("TesterA"));

        // Assert
        filterListCount.Should().NotBeNull();
        filterListCount.Should().Be(expectedList.Count);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        filterListCount = await repo.GetCountAsync(filter);

        // Assert
        filterListCount.Should().NotBeNull();
        filterListCount.Should().Be(expectedList.Count);
    }

    #endregion GetCountAsync

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNoEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        bool? actual = await repo.ExistsAsync(x => x.Id == Guid.NewGuid());

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(false);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCount_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        await context.TestEntities.InsertOneAsync(new TestEntity(Guid.NewGuid(), "TesterA", 34));
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        bool? actual = await repo.ExistsAsync(filter: u => u.Name.Equals("TesterA"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(true);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        actual = await repo.ExistsAsync(filter);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(true);
    }

    #endregion ExistsAsync

    #region InsertAsync

    [Fact]
    public async Task InsertAsync_ShouldReturnCount_WhenInserted()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expectedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.InsertAsync(expectedEntity);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(1);
        var actualEntity = await repo.GetByIdAsync(expectedEntity.Id);
        actualEntity.Should().NotBeNull();
        actualEntity.ShouldBeEquivalentToWithMilliseconds(expectedEntity);
    }

    #endregion InsertAsync

    #region InsertManyAsync

    [Fact]
    public async Task InsertManyAsync_ShouldReturnCount_WhenInserted()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actualCout = await repo.InsertManyAsync(expectedList);

        // Assert
        actualCout.Should().NotBeNull();
        actualCout.Should().Be(expectedList.Count);
        var actualList = await repo.GetListAsync(new ListQueryOptions<TestEntity>());
        actualList.Should().HaveCount(expectedList.Count);
        actualList.ShouldBeEquivalentToWithMilliseconds(expectedList);
    }

    #endregion InsertManyAsync

    #region UpdateByIdAsync

    [Fact]
    public async Task UpdateByIdAsync_ShouldReturnUpdated_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.InsertOneAsync(placedEntity);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.UpdateByIdAsync(placedEntity.Id, e => e.Name = "Updated");

        // Assert
        actual.Should().NotBeNull();
        actual.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateByIdAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.UpdateByIdAsync(Guid.NewGuid(), e => e.Name = "Updated"))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion UpdateByIdAsync

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldReturnCount_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.InsertOneAsync(placedEntity);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.UpdateAsync(new TestEntity(placedEntity.Id, "Updated", 10));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(1);
        (await repo.GetByIdAsync(placedEntity.Id)).Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.UpdateAsync(new TestEntity(Guid.NewGuid(), "Tester", 1)))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion UpdateAsync

    #region UpdateManyAsync

    [Fact]
    public async Task UpdateManyAsync_ShouldReturnCount_WhenEntitiesExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.InsertManyAsync(placedEntities);

        var updatedEntities = new List<TestEntity> { new(placedEntities[0].Id, "Updated", 20), new(placedEntities[1].Id, "Updated", 21) };
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.UpdateManyAsync(updatedEntities);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(updatedEntities.Count);
        (await repo.GetListAsync(new ListQueryOptions<TestEntity> { Filter = x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id) })).ShouldBeEquivalentToWithMilliseconds(updatedEntities);
    }

    [Fact]
    public async Task UpdateManyAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.UpdateManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "Tester", 61) }.AsReadOnly()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion UpdateManyAsync

    #region DeleteByIdAsync

    [Fact]
    public async Task DeleteByIdAsync_ShouldReturnCount_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.InsertOneAsync(placedEntity);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.DeleteByIdAsync(placedEntity.Id);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(1);
        (await repo.GetCountAsync(x => x.Id == placedEntity.Id)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.DeleteByIdAsync(Guid.NewGuid()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion DeleteByIdAsync

    #region DeleteByIdListAsync

    [Fact]
    public async Task DeleteByIdListAsync_ShouldReturnCount_WhenEntitiesExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.InsertManyAsync(placedEntities);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.DeleteByIdListAsync(placedEntities.Select(s => s.Id).ToList());

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);
    }

    [Fact]
    public async Task DeleteByIdListAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.DeleteByIdListAsync(new List<Guid> { Guid.NewGuid() }.AsReadOnly()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion DeleteByIdListAsync

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ShouldReturnCount_WhenEntityExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.InsertOneAsync(placedEntity);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.DeleteAsync(placedEntity);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(1);
        (await repo.GetCountAsync(x => x.Id == placedEntity.Id)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.DeleteAsync(new TestEntity(Guid.NewGuid(), "Tester", 1)))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion DeleteAsync

    #region DeleteManyAsync

    [Fact]
    public async Task DeleteManyAsync_ShouldReturnCount_WhenEntitiesExists()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.InsertManyAsync(placedEntities);
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.DeleteManyAsync(placedEntities);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);

        // Arrange
        await context.TestEntities.InsertManyAsync(placedEntities);


        // Act
        actual = await repo.DeleteManyAsync(x => x.Name.Equals("TesterA"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);

        // Arrange
        await context.TestEntities.InsertManyAsync(placedEntities);

        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        actual = await repo.DeleteManyAsync(filter);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);
    }

    [Fact]
    public async Task DeleteManyAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        using var context = new TestMongoDbContext($"{_connectionString}{Guid.NewGuid():N}");
        var repo = new MongoGenericRepository<TestMongoDbContext, TestEntity, Guid>(null, context);

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.DeleteManyAsync(new List<TestEntity> { new(Guid.NewGuid(), "Tester", 61) }.AsReadOnly()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();

        // Throw Act & Assert
        await FluentActions
            .Invoking(() => repo.DeleteManyAsync(x => x.Id == Guid.NewGuid()))
            .Should()
            .ThrowAsync<EntityNotFoundException>();
    }

    #endregion DeleteManyAsync
}