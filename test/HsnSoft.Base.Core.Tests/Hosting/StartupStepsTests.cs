using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Core.Tests.Hosting;

public class StartupStepsTests
{
    private static readonly TimeSpan s_noDelay = TimeSpan.Zero;

    [Fact]
    public async Task RunStartupStepAsync_RunsTheStepOnce_WhenItSucceeds()
    {
        int calls = 0;

        await new ServiceCollection().BuildServiceProvider().RunStartupStepAsync("step", _ => { calls++; return Task.CompletedTask; }, retryDelay: s_noDelay);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RunStartupStepAsync_RetriesUntilTheDependencyIsReady()
    {
        int calls = 0;
        ServiceProvider provider = new ServiceCollection().AddLogging().BuildServiceProvider();

        await provider.RunStartupStepAsync(
            "step",
            _ => ++calls < 3 ? throw new InvalidOperationException("not ready") : Task.CompletedTask,
            retryDelay: s_noDelay);

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task RunStartupStepAsync_AfterTheLastAttempt_ThrowsTheLastException()
    {
        int calls = 0;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new ServiceCollection().BuildServiceProvider().RunStartupStepAsync(
            "step",
            _ => throw new InvalidOperationException($"failure {++calls}"),
            maxAttempts: 3,
            retryDelay: s_noDelay));

        Assert.Equal(3, calls);
        Assert.Equal("failure 3", exception.Message);
    }

    [Fact]
    public async Task RunStartupStepAsync_UsesTheDefaultDelayBetweenAttempts()
    {
        int calls = 0;
        DateTime started = DateTime.UtcNow;

        await new ServiceCollection().BuildServiceProvider().RunStartupStepAsync(
            "step", _ => ++calls < 2 ? throw new InvalidOperationException("not ready") : Task.CompletedTask);

        Assert.InRange(DateTime.UtcNow - started, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task RunStartupStepAsync_ChecksItsArguments()
    {
        ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() => StartupSteps.RunStartupStepAsync(null!, "step", _ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.RunStartupStepAsync(" ", _ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.RunStartupStepAsync("step", null!));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.RunStartupStepAsync("step", _ => Task.CompletedTask, maxAttempts: 0));
    }

    [Fact]
    public void HealthCheckTags_Ready_IsTheAgreedName() => Assert.Equal("ready", HealthCheckTags.Ready);
}
