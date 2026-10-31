using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.AspNetCore.Tests;

public class HealthEndpointTests
{
    private static async Task<WebApplication> StartAsync(HealthStatus readyCheck, IConfiguration? configuration = null, Action<IHealthChecksBuilder>? more = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseHealthChecks(configuration);
        IHealthChecksBuilder checks = builder.Services.AddHealthChecks()
            .AddCheck("dependency", () => new HealthCheckResult(readyCheck, "the description", new InvalidOperationException("db-7.internal:5432 refused")), tags: ["ready"])
            .AddCheck("untagged", () => HealthCheckResult.Unhealthy());
        more?.Invoke(checks);

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

    [Theory]
    [InlineData("/health/startup")]
    [InlineData("/health/ready")]
    public async Task StartupAndReady_AskTheSameQuestion(string path)
    {
        await using WebApplication healthy = await StartAsync(HealthStatus.Healthy);
        await using WebApplication down = await StartAsync(HealthStatus.Unhealthy);

        Assert.Equal(HttpStatusCode.OK, (await healthy.GetTestClient().GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await down.GetTestClient().GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task TheAnswer_IsJson_WithTheStatusAndEveryReadyCheck_ButNotTheMessageOfAnException()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Unhealthy);

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("db-7", body);
        using JsonDocument json = JsonDocument.Parse(body);
        Assert.Equal("Unhealthy", json.RootElement.GetProperty("status").GetString());
        JsonElement check = Assert.Single(json.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("dependency", check.GetProperty("name").GetString());
        Assert.Equal("Unhealthy", check.GetProperty("status").GetString());
        Assert.Equal("the description", check.GetProperty("description").GetString());
        Assert.False(check.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task TheMessageOfAnException_IsInTheAnswer_WhenTheSettingSaysSo()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Health:IncludeErrors"] = "true" }).Build();
        await using WebApplication app = await StartAsync(HealthStatus.Unhealthy, configuration);

        string body = await (await app.GetTestClient().GetAsync("/health/ready")).Content.ReadAsStringAsync();

        using JsonDocument json = JsonDocument.Parse(body);
        Assert.Contains("db-7", json.RootElement.GetProperty("checks")[0].GetProperty("error").GetString());
    }

    [Fact]
    public async Task Live_AnswersJson_WithNoChecks()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Unhealthy);

        using JsonDocument json = JsonDocument.Parse(await (await app.GetTestClient().GetAsync("/health/live")).Content.ReadAsStringAsync());

        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.Empty(json.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task ACheckThatHangs_IsUnhealthyAfterTheTimeout_AndItsTokenIsCancelled()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Health:CheckTimeout"] = "00:00:00.200" }).Build();
        bool cancelled = false;
        await using WebApplication app = await StartAsync(HealthStatus.Healthy, configuration, checks => checks.AddAsyncCheck("hanging", async token =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                throw;
            }

            return HealthCheckResult.Healthy();
        }, tags: ["ready"]));

        DateTime started = DateTime.UtcNow;
        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(3));
        Assert.True(cancelled);
    }

    [Fact]
    public async Task ACheckWithATimeoutOfItsOwn_KeepsIt()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Health:CheckTimeout"] = "00:00:00.100" }).Build();
        await using WebApplication app = await StartAsync(HealthStatus.Healthy, configuration, checks => checks.AddAsyncCheck("slow but allowed", async token =>
        {
            await Task.Delay(400, token);
            return HealthCheckResult.Healthy();
        }, tags: ["ready"], timeout: TimeSpan.FromSeconds(10)));

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TheDefaultTimeoutOfACheck_IsFiveSeconds()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Healthy);

        Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions> options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        Assert.All(options.Value.Registrations, registration => Assert.Equal(TimeSpan.FromSeconds(5), registration.Timeout));
    }

    [Fact]
    public async Task ACheckThatThrows_ShowsNoMessageOfItsException_UnlessTheSettingSaysSo()
    {
        static void Throwing(IHealthChecksBuilder checks) => checks.AddAsyncCheck("throwing", _ => throw new InvalidOperationException("mongo-2.internal:27017 timed out"), tags: ["ready"]);
        IConfiguration with = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Health:IncludeErrors"] = "true" }).Build();
        await using WebApplication hidden = await StartAsync(HealthStatus.Healthy, more: Throwing);
        await using WebApplication shown = await StartAsync(HealthStatus.Healthy, with, Throwing);

        string hiddenBody = await (await hidden.GetTestClient().GetAsync("/health/ready")).Content.ReadAsStringAsync();
        string shownBody = await (await shown.GetTestClient().GetAsync("/health/ready")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("mongo-2", hiddenBody);
        Assert.Contains("the check failed", hiddenBody);
        Assert.Contains("mongo-2", shownBody);
    }

    private sealed class OwnCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.FromResult(HealthCheckResult.Unhealthy("the api is down"));
    }

    [Fact]
    public async Task ACheckOfTheService_DecidesReadiness_AndIsInTheAnswer()
    {
        await using WebApplication app = await StartAsync(HealthStatus.Healthy, more: checks => checks.Services.AddBaseReadyCheck<OwnCheck>("billing-api"));

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("billing-api", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ACheckThatTheSettingsDisable_IsNotRun_WhateverItsName_AndTheOthersAre()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Health:Disabled:0"] = "Billing-Api" }).Build();
        await using WebApplication app = await StartAsync(HealthStatus.Healthy, configuration, checks => checks.Services.AddBaseReadyCheck<OwnCheck>("billing-api"));

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("billing-api", body);
        Assert.Contains("dependency", body);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WebApplicationExtensions.AddBaseHealthChecks(null!));
        Assert.Throws<ArgumentNullException>(() => WebApplicationExtensions.MapBaseHealth(null!));
        Assert.Throws<ArgumentNullException>(() => WebApplicationExtensions.AddBaseReadyCheck<OwnCheck>(null!, "x"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseReadyCheck<OwnCheck>(" "));
    }
}
