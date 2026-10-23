using HsnSoft.Base.Logging.Models;

namespace HsnSoft.Base.Logging.Tests;

public class LogHelperTests
{
    [Fact]
    public void LogWithoutAnException_HasTheGivenValues()
    {
        object reference = new { Id = 7 };

        FrameworkLogModel log = LogHelper.Generate("order shipped", reference, "SHIPPING", "corr-1");

        Assert.Equal("order shipped", log.Message);
        Assert.Same(reference, log.Reference);
        Assert.Equal("SHIPPING", log.Facility);
        Assert.Equal("corr-1", log.CorrelationId);
        Assert.Null(log.StackTrace);
        Assert.False(string.IsNullOrEmpty(log.LogId));
        Assert.True(DateTime.UtcNow - log.CreatedAtUtc < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void EveryLog_HasItsOwnId()
    {
        Assert.NotEqual(LogHelper.Generate("a").LogId, LogHelper.Generate("a").LogId);
    }

    [Fact]
    public void LogWithAnException_CarriesWhereItWasThrown()
    {
        Exception exception = Throw(() => throw new InvalidOperationException("outer", new ArgumentException("middle", new FormatException("inner"))));

        FrameworkLogModel log = LogHelper.Generate("failed", exception: exception);

        StackTraceLogDetail detail = Assert.IsType<StackTraceLogDetail>(log.StackTrace);
        Assert.Equal("outer middle inner", detail.ErrorDetail);
        Assert.False(string.IsNullOrEmpty(detail.StackMethodName));
    }

    [Fact]
    public void InnerExceptionsWithoutAMessage_AreLeftOut()
    {
        Exception exception = new InvalidOperationException("outer", new ArgumentException(string.Empty, new FormatException("inner")));

        FrameworkLogModel log = LogHelper.Generate("failed", exception: exception);

        Assert.Equal("outer inner", log.StackTrace!.ErrorDetail);
    }

    [Fact]
    public void ExceptionWithoutInner_IsJustItsMessage()
    {
        FrameworkLogModel log = LogHelper.Generate("failed", exception: new InvalidOperationException("only"));

        Assert.Equal("only", log.StackTrace!.ErrorDetail);
        Assert.Null(log.StackTrace.StackFileName);
        Assert.Equal(0, log.StackTrace.StackLineNumber);
    }

    [Fact]
    public void MessageIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => LogHelper.Generate(null!));
    }

    private static Exception Throw(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("nothing was thrown");
    }
}
