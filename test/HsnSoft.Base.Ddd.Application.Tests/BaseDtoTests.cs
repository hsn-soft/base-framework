using HsnSoft.Base.Application.Dtos;

namespace HsnSoft.Base.Ddd.Application.Tests;

public class BaseDtoTests
{
    private sealed class OneDto : EntityDto<Guid>;

    private sealed class AuditedDto : AuditedEntityDto<Guid>;

    [Fact]
    public void EveryResponseBaseOfTheFramework_IsABaseDto()
    {
        Assert.IsType<IBaseDto>(new OneDto(), exactMatch: false);
        Assert.IsType<IBaseDto>(new AuditedDto(), exactMatch: false);
        Assert.IsType<IBaseDto>(new ListDataResultDto<OneDto>(), exactMatch: false);
        Assert.IsType<IBaseDto>(new PagedDataResultDto<OneDto>(), exactMatch: false);
    }

    [Fact]
    public void ARequest_IsNotHeldToTheMarker_ItIsFreeToBeOne()
    {
        Assert.False(typeof(IBaseDto).IsAssignableFrom(typeof(PagedDataRequestDto)));
        Assert.False(typeof(IBaseDto).IsAssignableFrom(typeof(LimitedDataRequestDto)));
    }
}
