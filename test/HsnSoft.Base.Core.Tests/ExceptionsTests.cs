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

    [Fact]
    public void KnownExceptions_AreTheOnesTheCallerCausedAndNoOtherOnes()
    {
        Assert.True(KnownExceptions.IsKnown(new NotFoundException("x")));
        Assert.True(KnownExceptions.IsKnown(new BusinessRuleException("x")));
        Assert.True(KnownExceptions.IsKnown(new BusinessException("code", "x")));
        Assert.True(KnownExceptions.IsKnown(new DomainException("x")));
        Assert.True(KnownExceptions.IsKnown(new ArgumentNullException("x")));
        Assert.True(KnownExceptions.IsKnown(new UnauthorizedAccessException()));
        Assert.True(KnownExceptions.IsKnown(new BaseHttpException(409)));
        Assert.True(KnownExceptions.IsKnown(new DerivedBusinessException()));

        Assert.False(KnownExceptions.IsKnown(new BaseHttpException(503)));
#pragma warning disable CA2201 // the exception a bug throws, on purpose
        Assert.False(KnownExceptions.IsKnown(new NullReferenceException()));
#pragma warning restore CA2201
        Assert.False(KnownExceptions.IsKnown(new InvalidOperationException()));
        Assert.False(KnownExceptions.IsKnown(new TimeoutException()));
        Assert.Throws<ArgumentNullException>(() => KnownExceptions.IsKnown(null!));
    }

    private sealed class DerivedBusinessException : BusinessException;

    [Fact]
    public void AnErrorThatNamesAText_KeepsItsMessage_AndCarriesTheKeyAndTheArguments()
    {
        NotFoundException notFound = new NotFoundException("order 7 is missing").WithKey("order_not_found", 7);
        BusinessRuleException rule = new BusinessRuleException("not allowed").WithKey("not_allowed");
        BaseHttpException http = new BaseHttpException(401, "no").WithKey("invalid_credentials", "a", "b");
        DomainException domain = new DomainException("broken").WithKey("field_required", "Name", 64);

        Assert.Equal("order 7 is missing", notFound.Message);
        Assert.Equal("order_not_found", notFound.MessageKey);
        Assert.Equal<object?>([7], [.. notFound.MessageArgs]);
        Assert.Equal(("not_allowed", 0), (rule.MessageKey, rule.MessageArgs.Count));
        Assert.Equal(("invalid_credentials", 2), (http.MessageKey, http.MessageArgs.Count));
        Assert.Equal("field_required", domain.MessageKey);
        Assert.Equal<object?>(["Name", 64], [.. domain.MessageArgs]);
        Assert.All<ILocalizableError>([notFound, rule, http, domain], error => Assert.NotNull(error.MessageKey));
    }

    [Fact]
    public void AnErrorWithoutAKey_HasNone_AndTheCodeOfABusinessExceptionIsItsKeyUnlessAnotherIsGiven()
    {
        Assert.Null(new DomainException("x").MessageKey);
        Assert.Empty(new NotFoundException("x").MessageArgs);
        Assert.Null(new BusinessException().MessageKey);

        BusinessException coded = new("stock_empty", "the stock is empty");
        Assert.Equal("stock_empty", coded.MessageKey);
        Assert.Empty(coded.MessageArgs);

        coded.WithKey("other_key", 1);
        Assert.Equal("other_key", coded.MessageKey);
        Assert.Equal<object?>([1], [.. coded.MessageArgs]);
    }
}
