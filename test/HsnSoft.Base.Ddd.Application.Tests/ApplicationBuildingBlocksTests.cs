using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Ddd.Application.Tests;

public class ApplicationBuildingBlocksTests
{
    private sealed class Row
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class RowDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; init; } = string.Empty;
    }

    private static readonly SortFields<Row> s_fields = new SortFields<Row>().Add("name", row => row.Name);

    private static List<ValidationResult> Validate(LimitedDataRequestDto request, IServiceProvider? services = null)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request, services, null), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void TheEntityDtos_AreBuiltUpFromTheIdToTheDeletion()
    {
        RowDto dto = new() { Id = Guid.NewGuid(), Name = "x", CreatedAt = DateTimeOffset.UnixEpoch, CreatedBy = Guid.NewGuid(), IsDeleted = true };

        Assert.IsAssignableFrom<AuditedEntityDto<Guid>>(dto);
        Assert.IsAssignableFrom<CreationAuditedEntityDto<Guid>>(dto);
        Assert.IsAssignableFrom<IEntityDto<Guid>>(dto);
        Assert.True(dto.IsDeleted);
        Assert.Null(dto.DeletedBy);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void TheRequests_DefaultToTenPerPage_FromTheFirstPage_AndAreOneChain()
    {
        PagedDataRequestDto request = new();

        Assert.Equal((10, 1, null, null), (request.MaxResultCount, request.PageNumber, request.SortingText, request.SearchText));
        Assert.IsAssignableFrom<IPagedDataRequest>(request);
        Assert.IsAssignableFrom<ISearchDataRequest>(request);
        Assert.IsAssignableFrom<ISortedDataRequest>(request);
        Assert.IsAssignableFrom<ILimitedDataRequest>(request);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AListHasAtLeastOneItem(int count) => Assert.NotEmpty(Validate(new LimitedDataRequestDto { MaxResultCount = count }));

    [Fact]
    public void AListHasAtMostTheLimit_100ByDefault_AndTheSettingChangesIt()
    {
        Assert.Empty(Validate(new LimitedDataRequestDto { MaxResultCount = 100 }));
        Assert.Contains(Validate(new LimitedDataRequestDto { MaxResultCount = 101 }), result => result.MemberNames.Contains(nameof(LimitedDataRequestDto.MaxResultCount)));

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Paging:MaxResultCountLimit"] = "20" }).Build();
        using ServiceProvider provider = new ServiceCollection().AddBasePaging(configuration).BuildServiceProvider();

        Assert.Empty(Validate(new LimitedDataRequestDto { MaxResultCount = 20 }, provider));
        Assert.NotEmpty(Validate(new LimitedDataRequestDto { MaxResultCount = 21 }, provider));
        Assert.Equal(20, provider.GetRequiredService<IOptions<PagingOptions>>().Value.MaxResultCountLimit);
    }

    [Fact]
    public void ThePagingSettings_AreOptional()
    {
        using ServiceProvider provider = new ServiceCollection().AddBasePaging().BuildServiceProvider();

        Assert.Equal(100, provider.GetRequiredService<IOptions<PagingOptions>>().Value.MaxResultCountLimit);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBasePaging());
    }

    [Fact]
    public void ARequestBecomesTheOptionsOfARepository()
    {
        PagedDataRequestDto paged = new() { PageNumber = 3, MaxResultCount = 20, SortingText = "name desc" };
        SortedAndLimitedDataRequestDto listed = new() { MaxResultCount = 5 };

        PagedQueryOptions<Row> pagedOptions = paged.ToPagedOptions<Row>(row => row.Name != "", s_fields, "name asc");
        ListQueryOptions<Row> listOptions = listed.ToListOptions<Row>(row => true, s_fields, "name asc");

        Assert.Equal((3, 20), (pagedOptions.PageNumber, pagedOptions.MaxResultCount));
        Assert.NotNull(pagedOptions.Filter);
        Assert.Single(pagedOptions.Sort!.Clauses);
        Assert.Equal(5, listOptions.MaxResultCount);
        Assert.Single(listOptions.Sort!.Clauses);
        Assert.Throws<ArgumentNullException>(() => paged.ToPagedOptions<Row>(row => true, null!, "name"));
        Assert.Throws<ArgumentNullException>(() => ((ISortedDataRequest)null!).ToListOptions<Row>(row => true, s_fields, "name"));
        Assert.Throws<ArgumentNullException>(() => listed.ToListOptions<Row>(row => true, null!, "name"));
        Assert.Throws<ArgumentNullException>(() => ((IPagedDataRequest)null!).ToPagedOptions<Row>(row => true, s_fields, "name"));
    }

    [Fact]
    public void APageResult_SaysWhereThePageIsAmongAll()
    {
        PagedDataResultDto<string> second = new(25, 2, 10, ["k", "l"]);

        Assert.Equal((25L, 3, 11, 12), (second.DataTotalCount, second.PageTotalCount, second.ResultDataStartNumber, second.ResultDataEndNumber));
        Assert.Equal(["k", "l"], second.Items);
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(5, 3, 10)]
    [InlineData(-4, 0, 0)]
    public void APageBeyondTheEnd_OrWithBadNumbers_IsEmpty(long total, int pageNumber, int pageSize)
    {
        PagedDataResultDto<string> page = new(total, pageNumber, pageSize, []);

        Assert.Equal((0, 0, 0), (page.PageTotalCount, page.ResultDataStartNumber, page.ResultDataEndNumber));
        Assert.Empty(page.Items);
    }

    [Fact]
    public void AListResult_IsNeverNull()
    {
        ListDataResultDto<string> list = new() { Items = null! };

        Assert.Empty(list.Items);
        Assert.Equal(["a"], new ListDataResultDto<string>(["a"]).Items);
    }

    [Fact]
    public void ARepositoryPage_IsSentAsAPageOfDtos()
    {
        PagedQueryResult<Row> result = new([new Row { Name = "a" }, new Row { Name = "b" }], 12);
        PagedDataRequestDto request = new() { PageNumber = 2, MaxResultCount = 5 };

        PagedDataResultDto<string> dto = result.ToPagedDto(request, row => row.Name.ToUpperInvariant());

        Assert.Equal(["A", "B"], dto.Items);
        Assert.Equal((12L, 3, 6, 7), (dto.DataTotalCount, dto.PageTotalCount, dto.ResultDataStartNumber, dto.ResultDataEndNumber));
        Assert.Throws<ArgumentNullException>(() => ((PagedQueryResult<Row>)null!).ToPagedDto(request, row => row.Name));
        Assert.Throws<ArgumentNullException>(() => result.ToPagedDto(null!, row => row.Name));
        Assert.Throws<ArgumentNullException>(() => result.ToPagedDto<Row, string>(request, null!));
    }

    [Fact]
    public void TheLimitCheck_NeedsAContext() => Assert.Throws<ArgumentNullException>(() => new LimitedDataRequestDto().Validate(null!).ToList());
}
