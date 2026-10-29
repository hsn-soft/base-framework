using Testcontainers.Redis;

namespace HsnSoft.Base.IntegrationTests;

public sealed class ContainerDiagnosticsTests
{
    [Fact]
    public async Task AHealthyContainer_SaysNothing_AndOneThatIsGone_SaysHowItEnded()
    {
        RedisContainer container = new RedisBuilder("redis:7").Build();
        await container.StartAsync();
        try
        {
            StringWriter healthy = new();
            await Support.ReportAsync(container, healthy);
            Assert.Equal(string.Empty, healthy.ToString());

            await container.StopAsync();
            StringWriter gone = new();
            await Support.ReportAsync(container, gone);

            Assert.Contains("[container-diagnostics]", gone.ToString(), StringComparison.Ordinal);
            Assert.Contains("state Exited", gone.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            await Support.DisposeAsync(container, new StringWriter());
        }
    }
}
