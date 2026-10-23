using HsnSoft.Base.Communication;

namespace HsnSoft.Base.Core.Tests;

public class ExceptionsTests
{
    [Fact]
    public void NotFoundException_KeepsTheMessage() => Assert.Equal("missing", new NotFoundException("missing").Message);

    [Fact]
    public void BusinessRuleException_KeepsTheMessage() => Assert.Equal("rule", new BusinessRuleException("rule").Message);

    [Fact]
    public void BusinessException_HasACodeData_AndALevel()
    {
        InvalidOperationException inner = new("inner");

        BusinessException exception = new("order_closed", "The order is closed.", inner, Microsoft.Extensions.Logging.LogLevel.Error);
        exception.WithData("OrderId", 7).WithData("Limit", null);

        Assert.Equal("order_closed", exception.ErrorCode);
        Assert.Equal("The order is closed.", exception.Message);
        Assert.Same(inner, exception.InnerException);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, exception.LogLevel);
        Assert.Equal(7, exception.Data["OrderId"]);
        Assert.Null(exception.Data["Limit"]);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, new BusinessException().LogLevel);
        Assert.Equal(string.Empty, new BusinessException().Message);
    }

    [Fact]
    public void DomainException_KeepsMessageInnerAndData()
    {
        InvalidOperationException inner = new("inner");

        Assert.NotNull(new DomainException().Message);
        Assert.Equal(string.Empty, new DomainException((string?)null).Message);
        Assert.Equal("broken", new DomainException("broken").Message);
        DomainException withInner = new("broken", inner);
        Assert.Same(inner, withInner.InnerException);
        Assert.Equal(string.Empty, new DomainException(null, null).Message);
        Assert.Equal("v", withInner.WithData("k", "v").Data["k"]);
    }

    [Fact]
    public void BaseHttpException_CarriesItsStatus()
    {
        Assert.Equal(418, new BaseHttpException(418, "teapot").HttpStatusCode);
        Assert.Equal("teapot", new BaseHttpException(418, "teapot").Message);
        Assert.Equal(string.Empty, new BaseHttpException(409).Message);
    }

    [Fact]
    public void BaseResponse_JoinsItsMessages_AndCarriesAPayload()
    {
        BaseResponse<int> response = new() { StatusCode = 200, StatusMessages = ["a", "b"], Payload = 7, TraceId = "t", ErrorCode = "e" };

        Assert.Equal("a, b", response.StatusMessagesToSingleMessage());
        Assert.Equal(7, ((IBaseResponse<int>)response).Payload);
        Assert.Equal(200, ((IBaseResponse)response).StatusCode);
        Assert.Empty(new BaseResponse().StatusMessages);
        Assert.Null(new BaseResponse().ErrorCode);
    }
}
