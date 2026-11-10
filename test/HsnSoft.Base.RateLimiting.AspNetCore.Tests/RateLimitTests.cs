using System.Net;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.RateLimiting.AspNetCore.Tests;

[ApiController]
[Route("api")]
public sealed class WorkController : ControllerBase
{
    [HttpGet("sign-in")]
    [RateLimit("sign-in", 3)]
    public string SignIn() => "in";

    [HttpGet("token")]
    [RateLimit("sign-in", 3)]
    public string Token() => "token";

    [HttpGet("slow")]
    [RateLimit("slow", 1, PerSeconds = 120)]
    public string Slow() => "slow";

    [HttpGet("mine")]
    [RateLimit("mine", 2, By = RateLimitBy.User)]
    public string Mine() => "mine";

    [HttpGet("free")]
    public string Free() => "free";
}

public sealed class ManualTime : TimeProvider
{
    private DateTimeOffset _now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Pass(TimeSpan span) => _now += span;
}

public sealed class TestUser : ICurrentUser
{
    public bool IsAuthenticated => Id is not null;

    public Guid? Id { get; set; }

    public string? UserName => Id?.ToString("N");
}

public sealed class BrokenLimiter : IRateLimiter
{
    public ValueTask<RateLimitResult> TryAcquireAsync(string name, string key, int permits, TimeSpan window, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("the store of the counts is gone");
}

public class RateLimitTests
{
    private static async Task<(WebApplication App, ManualTime Time)> StartAsync(Dictionary<string, string?>? settings = null, Action<IServiceCollection>? services = null, bool global = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        ManualTime time = new();
        builder.Services.AddSingleton<TimeProvider>(time).AddInMemoryRateLimiter(builder.Configuration);
        builder.Services.AddSingleton<TestUser>().AddSingleton<ICurrentUser>(provider => provider.GetRequiredService<TestUser>());
        builder.Services.AddControllers().AddApplicationPart(typeof(WorkController).Assembly);
        services?.Invoke(builder.Services);
        WebApplication app = builder.Build();
        // the address of the caller comes from a header of the test, as the proxies of a host would give it
        app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Test-Ip", out Microsoft.Extensions.Primitives.StringValues ip))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            return next(context);
        });
        if (global)
        {
            app.UseBaseRateLimiting();
        }

        app.MapControllers();
        app.MapGet("/minimal", () => "minimal").RequireRateLimit("minimal", 1);
        app.MapGet("/health/live", () => "alive");
        await app.StartAsync();
        return (app, time);
    }

    private static Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string ip = "10.0.0.1")
    {
        HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Add("X-Test-Ip", ip);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task ACallerMayMakeThePermits_TheNextIsTold429WithRetryAfter_AndAnotherCallerIsNotAffected()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            for (int request = 0; request < 3; request++)
            {
                Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/sign-in")).StatusCode);
            }

            HttpResponseMessage refused = await GetAsync(client, "/api/sign-in");
            BaseResponse? body = await refused.Content.ReadFromJsonAsync<BaseResponse>();

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.Equal(60, refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
            Assert.Equal((429, "rate_limit_exceeded"), (body!.StatusCode, body.ErrorCode));
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/sign-in", ip: "10.0.0.2")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/free")).StatusCode);   // an endpoint with no limit
        }
    }

    [Fact]
    public async Task TheWindowEnds_AndTheCallerStartsAgain_AndTheWindowOfTheAttributeIsUsed()
    {
        (WebApplication app, ManualTime time) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/slow")).StatusCode);

            HttpResponseMessage refused = await GetAsync(client, "/api/slow");
            time.Pass(TimeSpan.FromSeconds(119));
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(client, "/api/slow")).StatusCode);
            time.Pass(TimeSpan.FromSeconds(2));

            Assert.Equal(120, refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/slow")).StatusCode);
        }
    }

    [Fact]
    public async Task TwoEndpointsWithOneName_ShareTheCountOfACaller()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/sign-in")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/token")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/sign-in")).StatusCode);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(client, "/api/token")).StatusCode);
        }
    }

    [Fact]
    public async Task TheSettings_ChangeThePermits_WithoutADeployment()
    {
        (WebApplication app, _) = await StartAsync(new Dictionary<string, string?> { ["RateLimiting:Limits:sign-in:Permits"] = "1" });
        await using (app)
        {
            HttpClient client = app.GetTestClient();

            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/sign-in")).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(client, "/api/sign-in")).StatusCode);
        }
    }

    [Fact]
    public async Task AMinimalApiEndpoint_HasTheSameLimit()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();

            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/minimal")).StatusCode);
            HttpResponseMessage refused = await GetAsync(client, "/minimal");
            BaseResponse? body = await refused.Content.ReadFromJsonAsync<BaseResponse>();

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.NotNull(refused.Headers.RetryAfter);
            Assert.Equal("rate_limit_exceeded", body!.ErrorCode);
        }
    }

    [Fact]
    public async Task ALimitByUser_CountsEachUserApart_AndACallerThatIsNotSignedInByItsAddress()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            TestUser user = app.Services.GetRequiredService<TestUser>();
            user.Id = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/mine")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/mine", ip: "10.0.0.9")).StatusCode);   // the same user from another address: one count
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(client, "/api/mine")).StatusCode);

            user.Id = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/mine")).StatusCode);

            user.Id = null;
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/mine", ip: "10.0.0.5")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/mine", ip: "10.0.0.5")).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(client, "/api/mine", ip: "10.0.0.5")).StatusCode);
        }
    }

    [Fact]
    public async Task TheGlobalLimit_CountsEveryRequestOfAnAddress_ButNotTheExcludedPaths()
    {
        (WebApplication app, _) = await StartAsync(new Dictionary<string, string?> { ["RateLimiting:Global:Permits"] = "2" }, global: true);
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            for (int probe = 0; probe < 5; probe++)
            {
                Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/health/live")).StatusCode);   // a probe is never refused
            }

            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/free")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/free")).StatusCode);
            HttpResponseMessage refused = await GetAsync(client, "/api/free");
            BaseResponse? body = await refused.Content.ReadFromJsonAsync<BaseResponse>();

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.Equal(60, refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
            Assert.Equal((429, "rate_limit_exceeded"), (body!.StatusCode, body.ErrorCode));
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/free", ip: "10.0.0.2")).StatusCode);
        }
    }

    [Fact]
    public async Task WithNoPermitsThereIsNoGlobalLimit_AndAWindowThatCannotBeRightStopsTheHost()
    {
        (WebApplication app, _) = await StartAsync(global: true);
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            for (int request = 0; request < 20; request++)
            {
                Assert.Equal(HttpStatusCode.OK, (await GetAsync(client, "/api/free")).StatusCode);
            }
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await StartAsync(new Dictionary<string, string?> { ["RateLimiting:Global:Permits"] = "5", ["RateLimiting:Global:WindowSeconds"] = "0" }, global: true));
    }

    [Fact]
    public async Task WhenTheStoreIsGone_TheRequestFails_UnlessTheSettingsLetItThrough()
    {
        (WebApplication closed, _) = await StartAsync(services: services => services.AddSingleton<IRateLimiter, BrokenLimiter>());
        await using (closed)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await GetAsync(closed.GetTestClient(), "/api/sign-in"));
        }

        (WebApplication open, _) = await StartAsync(new Dictionary<string, string?> { ["RateLimiting:FailOpen"] = "true" }, services => services.AddSingleton<IRateLimiter, BrokenLimiter>());
        await using (open)
        {
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(open.GetTestClient(), "/api/sign-in")).StatusCode);
        }
    }
}
