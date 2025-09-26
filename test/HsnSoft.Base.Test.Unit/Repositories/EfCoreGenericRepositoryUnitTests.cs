using FluentAssertions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Test.Unit.Models;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Unit.Repositories;

public class EfCoreGenericRepositoryUnitTests
{
    private readonly DbContextOptions<TestEfCoreDbContext> _dbContextOptions = new DbContextOptionsBuilder<TestEfCoreDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturn_WhenEntityExists()
    {
        // Arrange
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.AddAsync(expected);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.AddAsync(expected);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.AddAsync(expected);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        await context.TestEntities.AddRangeAsync(new TestEntity(Guid.NewGuid(), "Tester", 61), new TestEntity(Guid.NewGuid(), "Tester", 61));
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.AddAsync(expected);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetSingleOrDefaultAsync(e => e.Id == Guid.NewGuid());

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetSingleOrDefaultAsync_ShouldThrow_WhenEntityDuplicate()
    {
        // Arrange
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        await context.TestEntities.AddRangeAsync(new TestEntity(Guid.NewGuid(), "Tester", 61), new TestEntity(Guid.NewGuid(), "Tester", 61));
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expected = new TestEntity(Guid.NewGuid(), "Tester", 61);
        await context.TestEntities.AddRangeAsync(new TestEntity(Guid.NewGuid(), "Tester", 18), expected);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        await context.TestEntities.AddRangeAsync(expectedList);
        await context.TestEntities.AddRangeAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 21), new(Guid.NewGuid(), "TesterC", 22), new(Guid.NewGuid(), "TesterD", 23) });
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

        // Act
        var noFilterList = await repo.GetListAsync(new ListQueryOptions<TestEntity>());

        // Assert
        noFilterList.Should().NotBeNull();
        noFilterList.Should().HaveCount(5);

        // Act
        var limitedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { MaxResultCount = 3 });

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
        var filterIdList = await repo.GetListAsync(selector: x => x.Id, options: new ListQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterIdList.Should().NotBeNull();
        filterIdList.Should().HaveCount(expectedList.Count);
        filterIdList.Should().BeEquivalentTo(expectedList.Select(x => x.Id));

        // Act
        var filterObjectList = await repo.GetListAsync(selector: x => new { x.Id, x.Name }, options: new ListQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterObjectList.Should().NotBeNull();
        filterObjectList.Should().HaveCount(expectedList.Count);
        filterObjectList.Should().BeEquivalentTo(expectedList.Select(x => new { x.Id, x.Name }));

        // Act
        var dynamicOrderedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { OrderByDynamic = $"{nameof(TestEntity.Name)} asc, {nameof(TestEntity.Age)} desc", MaxResultCount = 1 });

        // Assert
        dynamicOrderedList.Should().NotBeNull();
        dynamicOrderedList.Should().HaveCount(1);
        dynamicOrderedList[0].Name.Should().Be("TesterA");
        dynamicOrderedList[0].Age.Should().Be(12);

        // Act
        var orderedList = await repo.GetListAsync(options: new ListQueryOptions<TestEntity> { OrderByEntity = o => o.OrderBy(e => e.Name).ThenByDescending(a => a.Age), MaxResultCount = 1 });

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

        // Act
        var actual = await repo.GetPageListAsync(new PagedQueryOptions<TestEntity>());

        // Assert
        actual.Should().NotBeNull();
        actual.Items.Should().BeEmpty();
        actual.TotalCount.Should().Be(0);
        actual.ResultPageNumber.Should().Be(1);
        actual.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);
    }

    [Fact]
    public async Task GetPageListAsync_ShouldReturnList_WhenEntityExists()
    {
        // Arrange
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        await context.TestEntities.AddRangeAsync(expectedList);
        await context.TestEntities.AddRangeAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 21), new(Guid.NewGuid(), "TesterC", 22), new(Guid.NewGuid(), "TesterD", 23) });
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

        // Act
        var noFilterPageList = await repo.GetPageListAsync(new PagedQueryOptions<TestEntity>());

        // Assert
        noFilterPageList.Should().NotBeNull();
        noFilterPageList.Items.Should().HaveCount(5);
        noFilterPageList.TotalCount.Should().Be(5);
        noFilterPageList.ResultPageNumber.Should().Be(1);
        noFilterPageList.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);

        // Act
        var limitedPageList = await repo.GetPageListAsync(options: new PagedQueryOptions<TestEntity> { ResultPageNumber = 3, MaxResultCount = 2 });

        // Assert
        limitedPageList.Should().NotBeNull();
        limitedPageList.Items.Should().HaveCount(1);
        limitedPageList.TotalCount.Should().Be(5);
        limitedPageList.ResultPageNumber.Should().Be(3);
        limitedPageList.MaxResultCount.Should().Be(2);

        // Act
        var filterPageList = await repo.GetPageListAsync(options: new PagedQueryOptions<TestEntity> { Filter = u => u.Name.Equals("TesterA") });

        // Assert
        filterPageList.Should().NotBeNull();
        filterPageList.Items.Should().HaveCount(2);
        filterPageList.TotalCount.Should().Be(2);
        filterPageList.ResultPageNumber.Should().Be(1);
        filterPageList.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);
        filterPageList.Items.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Arrange
        var filter = new FilterBuilder<TestEntity>()
            .And(x => x.Name.Equals("TesterA"))
            .Build();

        // Act
        filterPageList = await repo.GetPageListAsync(options: new PagedQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterPageList.Should().NotBeNull();
        filterPageList.Items.Should().HaveCount(2);
        filterPageList.TotalCount.Should().Be(2);
        filterPageList.ResultPageNumber.Should().Be(1);
        filterPageList.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);
        filterPageList.Items.ShouldBeEquivalentToWithMilliseconds(expectedList);

        // Act
        var filterPageIdList = await repo.GetPageListAsync(selector: x => x.Id, options: new PagedQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterPageIdList.Should().NotBeNull();
        filterPageIdList.Items.Should().HaveCount(2);
        filterPageIdList.TotalCount.Should().Be(2);
        filterPageIdList.ResultPageNumber.Should().Be(1);
        filterPageIdList.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);
        filterPageIdList.Items.Should().BeEquivalentTo(expectedList.Select(x => x.Id));

        // Act
        var filterPageObjectList = await repo.GetPageListAsync(selector: x => new { x.Id, x.Name }, options: new PagedQueryOptions<TestEntity> { Filter = filter });

        // Assert
        filterPageObjectList.Should().NotBeNull();
        filterPageObjectList.Items.Should().HaveCount(2);
        filterPageObjectList.TotalCount.Should().Be(2);
        filterPageObjectList.ResultPageNumber.Should().Be(1);
        filterPageObjectList.MaxResultCount.Should().Be(new PagedQueryOptions<TestEntity>().MaxResultCount);
        filterPageObjectList.Items.Should().BeEquivalentTo(expectedList.Select(x => new { x.Id, x.Name }));

        // Act
        var dynamicOrderedPageList = await repo.GetPageListAsync(options: new PagedQueryOptions<TestEntity> { OrderByDynamic = $"{nameof(TestEntity.Name)} asc, {nameof(TestEntity.Age)} desc", MaxResultCount = 1 });

        // Assert
        dynamicOrderedPageList.Should().NotBeNull();
        dynamicOrderedPageList.Items.Should().HaveCount(1);
        dynamicOrderedPageList.Items[0].Name.Should().Be("TesterA");
        dynamicOrderedPageList.Items[0].Age.Should().Be(12);
        dynamicOrderedPageList.TotalCount.Should().Be(5);
        dynamicOrderedPageList.ResultPageNumber.Should().Be(1);
        dynamicOrderedPageList.MaxResultCount.Should().Be(1);

        // Act
        var orderedPageList = await repo.GetPageListAsync(options: new PagedQueryOptions<TestEntity> { OrderByEntity = o => o.OrderBy(e => e.Name).ThenByDescending(a => a.Age), MaxResultCount = 1 });

        // Assert
        orderedPageList.Should().NotBeNull();
        orderedPageList.Items.Should().HaveCount(1);
        orderedPageList.Items[0].Name.Should().Be("TesterA");
        orderedPageList.Items[0].Age.Should().Be(12);
        orderedPageList.TotalCount.Should().Be(5);
        orderedPageList.ResultPageNumber.Should().Be(1);
        orderedPageList.MaxResultCount.Should().Be(1);
    }

    #endregion GetPageListAsync

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_ShouldReturnZero_WhenNoEntityExists()
    {
        // Arrange
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 34), new(Guid.NewGuid(), "TesterA", 61) };
        await context.TestEntities.AddRangeAsync(expectedList);
        await context.TestEntities.AddRangeAsync(new List<TestEntity> { new(Guid.NewGuid(), "TesterB", 1), new(Guid.NewGuid(), "TesterC", 2), new(Guid.NewGuid(), "TesterD", 3) });
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        await context.TestEntities.AddAsync(new TestEntity(Guid.NewGuid(), "TesterA", 34));
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expectedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var expectedList = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 11), new(Guid.NewGuid(), "TesterA", 12) };
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.AddAsync(placedEntity);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.AddAsync(placedEntity);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.AddRangeAsync(placedEntities);
        await context.SaveChangesAsync();
        var updatedEntities = new List<TestEntity> { new(placedEntities[0].Id, "Updated", 20), new(placedEntities[1].Id, "Updated", 21) };
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.AddAsync(placedEntity);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.AddRangeAsync(placedEntities);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntity = new TestEntity(Guid.NewGuid(), "TesterA", 10);
        await context.TestEntities.AddAsync(placedEntity);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var placedEntities = new List<TestEntity> { new(Guid.NewGuid(), "TesterA", 10), new(Guid.NewGuid(), "TesterA", 11) };
        await context.TestEntities.AddRangeAsync(placedEntities);
        await context.SaveChangesAsync();
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

        // Act
        int? actual = await repo.DeleteManyAsync(placedEntities);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);

        // Arrange
        await context.TestEntities.AddRangeAsync(placedEntities);
        await context.SaveChangesAsync();

        // Act
        actual = await repo.DeleteManyAsync(x => x.Name.Equals("TesterA"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(placedEntities.Count);
        (await repo.GetCountAsync(x => placedEntities.Select(s => s.Id).ToList().Contains(x.Id))).Should().Be(0);

        // Arrange
        await context.TestEntities.AddRangeAsync(placedEntities);
        await context.SaveChangesAsync();
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
        await using var context = new TestEfCoreDbContext(_dbContextOptions);
        var repo = new EfCoreGenericRepository<TestEntity, Guid>(null, context);

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