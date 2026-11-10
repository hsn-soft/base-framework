using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

[ApiController]
[AllowAnonymous]
[SchedulerAuth]
[Route("api/jobs")]
public sealed class SchedulerJobsController : ControllerBase
{
    [HttpPost("run")]
    public IActionResult Run() => Ok(new { ran = true });
}

public sealed class SchedulerAuthTests
{
    private const string Key = "a-key-of-the-scheduler-0123456789";

    private static async Task<WebApplication> StartAsync(string? key = Key)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        if (key is not null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Scheduler:ApiKey"] = key });
        }

        builder.Services.AddBaseWebApi();
        builder.Services.AddBaseSchedulerAuth(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(SchedulerJobsController).Assembly);
        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpStatusCode> CallAsync(WebApplication app, string? key, string? header = SchedulerHeaders.ApiKey)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/jobs/run");
        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation(header!, key);
        }

        return (await app.GetTestClient().SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task TheKeyOfTheScheduler_LetsTheJobIn_WithNoToken()
    {
        await using WebApplication app = await StartAsync();

        Assert.Equal(HttpStatusCode.OK, await CallAsync(app, Key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("another-key")]
    [InlineData("a-key-of-the-scheduler-0123456780")]
    [InlineData("A-KEY-OF-THE-SCHEDULER-0123456789")]
    public async Task AMissingOrWrongKey_IsUnauthorized(string? sent)
    {
        await using WebApplication app = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync(app, sent));
    }

    [Fact]
    public async Task TheKey_IsOnlyReadFromItsHeader()
    {
        await using WebApplication app = await StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync(app, Key, "X-Api-Key"));
    }

    [Fact]
    public async Task TheAnswerOfARefusal_SaysWhatIsWrong_AndNeverTheKeyOrWhatWasSent()
    {
        await using WebApplication app = await StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/jobs/run");
        request.Headers.TryAddWithoutValidation(SchedulerHeaders.ApiKey, "guess");

        string body = await (await app.GetTestClient().SendAsync(request)).Content.ReadAsStringAsync();

        Assert.Contains("scheduler key is missing or wrong", body, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, body, StringComparison.Ordinal);
        Assert.DoesNotContain("guess", body, StringComparison.Ordinal);
    }

    private sealed class StopAtOnce(IHostApplicationLifetime lifetime) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            lifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static async Task<int> RunAsync(string? key)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        if (key is not null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Scheduler:ApiKey"] = key });
        }

        builder.Services.AddBaseSchedulerAuth(builder.Configuration);
        builder.Services.AddHostedService<StopAtOnce>();
        return await builder.RunBaseAsync((Action<WebApplication>)(_ => { }));
    }

    [Fact]
    public async Task AServiceWithNoKey_DoesNotStart_AndOneWithAKeyDoes()
    {
        Assert.Equal(1, await RunAsync(null));
        Assert.Equal(1, await RunAsync("  "));
        Assert.Equal(0, await RunAsync(Key));
    }

    [Fact]
    public async Task AHostThatFailsToStart_ReturnsOne_AndTheReasonIsNotLost()
    {
        TextWriter original = Console.Error;
        using StringWriter captured = new();
        Console.SetError(captured);
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddOptions<StartCheck>().Configure(check => check.Value = 0).Validate(check => check.Value > 0, "the value of the check has to be more than zero").ValidateOnStart();

            int exit = await builder.RunBaseAsync((Action<WebApplication>)(_ => { }));

            Assert.Equal(1, exit);   // not an ObjectDisposedException from the logger of a host that is gone
            Assert.Contains("the value of the check has to be more than zero", captured.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(original);
        }
    }

    private sealed class StartCheck
    {
        public int Value { get; set; }
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SchedulerAuthServiceCollectionExtensions.AddBaseSchedulerAuth(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseSchedulerAuth(null!));
    }
}
