using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.AspNetCore.Tests;

public class HealthEndpointTests
{
    private static async Task<WebApplication> StartAsync(HealthStatus readyCheck)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseHealthChecks();
        builder.Services.AddHealthChecks()
            .AddCheck("dependency", () => new HealthCheckResult(readyCheck), tags: ["ready"])
            .AddCheck("untagged", () => HealthCheckResult.Unhealthy());

        WebApplication app = builder.Build();
        app.MapBaseHealth();
        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task Live_IsOk_EvenWhenADependencyIsDown()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Unhealthy);

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_IsOk_WhenTheReadyChecksPass_AndIgnoresUntaggedChecks()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Healthy);

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_IsUnavailable_WhenAReadyCheckFails()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Unhealthy);

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WebApplicationExtensions.AddBaseHealthChecks(null!));
        Assert.Throws<ArgumentNullException>(() => WebApplicationExtensions.MapBaseHealth(null!));
    }
}
