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
