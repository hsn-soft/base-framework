using HsnSoft.Base.Logging;

namespace HsnSoft.Base.Core.Tests;

public sealed class LogTypesAndFlowTests
{
    [Fact]
    public void LogFlow_IsEmptyOutsideAStep_AndFollowsTheStepThatBegan()
    {
        Assert.Null(LogFlow.Current);

        using (LogFlow.Begin("corr", "scope-1", "token-login"))
        {
            Assert.Equal(new LogFlowStep("corr", "scope-1", "token-login"), LogFlow.Current);

            using (LogFlow.Begin("corr", "scope-2", "ordercreated"))
            {
                Assert.Equal("ordercreated", LogFlow.Current?.Name);
            }

            Assert.Equal("token-login", LogFlow.Current?.Name);
        }

        Assert.Null(LogFlow.Current);
    }

    [Fact]
    public async Task LogFlow_FollowsAsyncCalls()
    {
        using IDisposable step = LogFlow.Begin("corr", "scope", "handler");

        string? seen = await Task.Run(async () =>
        {
            await Task.Yield();
            return LogFlow.Current?.Name;
        });

        Assert.Equal("handler", seen);
    }

    [Fact]
    public void LogFlow_NeedsAScopeIdAndAName()
    {
        Assert.Throws<ArgumentException>(() => LogFlow.Begin("corr", " ", "name"));
        Assert.Throws<ArgumentException>(() => LogFlow.Begin("corr", "scope", ""));
    }

    [Fact]
    public void ChainFields_HaveTheSameNamesEverywhere()
    {
        Dictionary<string, object?> fields = LogFields.Chain(LogTypes.HttpRequest, "corr", "scope", LogFlow.HttpClientName, "token-login");

        Assert.Equal(
            new Dictionary<string, object?> { ["LogType"] = "HttpRequest", ["CorrelationId"] = "corr", ["ScopeId"] = "scope", ["From"] = "httpclient", ["To"] = "token-login" },
            fields);
        Assert.Equal("ElapsedMs", LogFields.ElapsedMs);
    }

    [Fact]
    public void NewId_IsLowerCaseHexWithoutDashes_AndUnique()
    {
        string first = LogFields.NewId();

        Assert.Matches("^[0-9a-f]{32}$", first);
        Assert.NotEqual(first, LogFields.NewId());
    }

    [Fact]
    public void ACorrelationId_IsTheTraceOfTheRequest_WhenThereIsOne_ElseANewId()
    {
        using System.Diagnostics.ActivitySource source = new("test-correlation");
        using System.Diagnostics.ActivityListener listener = new()
        {
            ShouldListenTo = candidate => candidate.Name == "test-correlation",
            Sample = (ref _) => System.Diagnostics.ActivitySamplingResult.AllData,
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        string withoutTrace = LogFields.NewCorrelationId();
        using System.Diagnostics.Activity? activity = source.StartActivity("request");
        string withTrace = LogFields.NewCorrelationId();

        Assert.Matches("^[0-9a-f]{32}$", withoutTrace);
        Assert.Equal(activity!.TraceId.ToHexString(), withTrace);
        Assert.True(LogFields.IsTraceId(withTrace));
    }

    [Theory]
    [InlineData("cc55d74f7c0c25f06cef2eb55e0c6c23", true)]
    [InlineData("00000000000000000000000000000000", false)]
    [InlineData("CC55D74F7C0C25F06CEF2EB55E0C6C23", false)]
    [InlineData("cc55d74f7c0c25f06cef2eb55e0c6c2", false)]
    [InlineData("zz55d74f7c0c25f06cef2eb55e0c6c23", false)]
    [InlineData("mobile-app-7", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ATraceId_Is32LowerCaseHexDigitsNotAllZero(string? value, bool expected)
        => Assert.Equal(expected, LogFields.IsTraceId(value));

    [Fact]
    public void StepName_IsControllerAndActionInLowerCase()
        => Assert.Equal("token-login", LogFields.StepName("Token", "Login"));

    [Fact]
    public void Text_IsCutAndMarked_NullAndShortTextStay()
    {
        Assert.Null(LogFields.Text(null, 5));
        Assert.Equal("abc", LogFields.Text("abc", 5));
        Assert.Equal("abcde", LogFields.Text("abcde", 5));
        Assert.Equal("abcde...(+3)", LogFields.Text("abcdefgh", 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => LogFields.Text("a", 0));
    }
}
