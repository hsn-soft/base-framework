using System.Net;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Concurrency.AspNetCore.Tests;

[ApiController]
[Route("api")]
public sealed class WorkController(WorkGate gate) : ControllerBase
{
    [HttpGet("send")]
    [ConcurrencyLimit("send", 2)]
    public async Task<string> SendAsync()
    {
        await gate.EnterAsync();
        return "sent";
    }

    [HttpGet("same-a")]
    [ConcurrencyLimit("shared", 1)]
    public async Task<string> SameAAsync()
    {
        await gate.EnterAsync();
        return "a";
    }

    [HttpGet("same-b")]
    [ConcurrencyLimit("shared", 1)]
    public string SameB() => "b";

    [HttpGet("fails")]
    [ConcurrencyLimit("fails", 1)]
    public string Fails() => throw new InvalidOperationException("the endpoint failed");

    // the action publishes an event: the first event takes the place (here the test does what the header provider of the event bus does)
    [HttpGet("hand-off")]
    [ConcurrencyLimit("hand", 1, HandOff = true)]
    public string HandOff([FromServices] HandoffBox box)
    {
        box.Taken = ConcurrencyHandoffContext.TryTake();
        return "published";
    }

    [HttpGet("hand-off-nothing")]
    [ConcurrencyLimit("hand-nothing", 1, HandOff = true)]
    public string HandOffNothing() => "nothing was published";

    [HttpGet("hand-off-fails")]
    [ConcurrencyLimit("hand-fails", 1, HandOff = true)]
    public string HandOffFails([FromServices] HandoffBox box)
    {
        box.Taken = ConcurrencyHandoffContext.TryTake();
        throw new InvalidOperationException("the broker refused the event");
    }

    [HttpGet("free")]
    public string Free() => "free";
}

/// <summary>What an action handed over (the place a published event took).</summary>
public sealed class HandoffBox
{
    public ConcurrencyHandoff? Taken { get; set; }
}

/// <summary>Holds the requests that are inside an endpoint until the test lets them go.</summary>
public sealed class WorkGate : IDisposable
{
    private readonly SemaphoreSlim _release = new(0);
    private int _inside;

    public int Inside => Volatile.Read(ref _inside);

    public async Task EnterAsync()
    {
        Interlocked.Increment(ref _inside);
        await _release.WaitAsync();
        Interlocked.Decrement(ref _inside);
    }

    public void Let(int count) => _release.Release(count);

    public void Dispose() => _release.Dispose();
}

public class ConcurrencyLimitEndpointTests
{
    private static async Task<(WebApplication App, WorkGate Gate)> StartAsync(Dictionary<string, string?>? settings = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        WorkGate gate = new();
        builder.Services.AddSingleton(gate).AddSingleton<HandoffBox>().AddInMemoryConcurrencyLimiter(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(WorkController).Assembly);
        WebApplication app = builder.Build();
        app.MapControllers();
        app.MapGet("/minimal", async (WorkGate work) =>
        {
            await work.EnterAsync();
            return "minimal";
        }).RequireConcurrencyLimit("minimal", 1);
        app.MapGet("/minimal-hand-off", (HandoffBox box) =>
        {
            box.Taken = ConcurrencyHandoffContext.TryTake();
            return "published";
        }).RequireConcurrencyLimit("minimal-hand", 1, handOff: true);
        await app.StartAsync();
        return (app, gate);
    }

    private static async Task WaitInsideAsync(WorkGate gate, int count)
    {
        for (int attempt = 0; attempt < 200 && gate.Inside < count; attempt++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(count, gate.Inside);
    }

    [Fact]
    public async Task AnEndpointHasNoMoreRunsAtOnceThanItsLimit_AndTheRestAreToldTooMany()
    {
        (WebApplication app, WorkGate gate) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Task<HttpResponseMessage> first = client.GetAsync("/api/send");
            Task<HttpResponseMessage> second = client.GetAsync("/api/send");
            await WaitInsideAsync(gate, 2);

            HttpResponseMessage refused = await client.GetAsync("/api/send");
            BaseResponse? body = await refused.Content.ReadFromJsonAsync<BaseResponse>();
            HttpResponseMessage unlimited = await client.GetAsync("/api/free");

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.Equal("1", refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal((429, "concurrency_limit_reached"), (body!.StatusCode, body.ErrorCode));
            Assert.Equal(HttpStatusCode.OK, unlimited.StatusCode);

            gate.Let(2);
            Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await second).StatusCode);
            gate.Let(1);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/send")).StatusCode);
        }
    }

    [Fact]
    public async Task TwoEndpointsWithOneName_ShareTheirPlaces()
    {
        (WebApplication app, WorkGate gate) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Task<HttpResponseMessage> holding = client.GetAsync("/api/same-a");
            await WaitInsideAsync(gate, 1);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/same-b")).StatusCode);

            gate.Let(1);
            await holding;
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/same-b")).StatusCode);
        }
    }

    [Fact]
    public async Task APlace_IsGivenBack_WhenTheEndpointFails()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();

            for (int call = 0; call < 3; call++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/api/fails"));
            }
        }
    }

    [Fact]
    public async Task TheSettings_ChangeTheMaximumWithoutAChangeOfCode_AndTheRetryAfter()
    {
        (WebApplication app, WorkGate gate) = await StartAsync(new() { ["Concurrency:Limits:send:Max"] = "1", ["Concurrency:RetryAfterSeconds"] = "5" });
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Task<HttpResponseMessage> holding = client.GetAsync("/api/send");
            await WaitInsideAsync(gate, 1);

            HttpResponseMessage refused = await client.GetAsync("/api/send");

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.Equal(5, refused.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
            gate.Let(1);
            await holding;
        }
    }

    [Fact]
    public async Task AMinimalApiEndpoint_HasTheSameLimit()
    {
        (WebApplication app, WorkGate gate) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            Task<HttpResponseMessage> holding = client.GetAsync("/minimal");
            await WaitInsideAsync(gate, 1);

            HttpResponseMessage refused = await client.GetAsync("/minimal");
            BaseResponse? body = await refused.Content.ReadFromJsonAsync<BaseResponse>();

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            Assert.NotNull(refused.Headers.RetryAfter);
            Assert.Equal("concurrency_limit_reached", body!.ErrorCode);
            gate.Let(1);
            Assert.Equal(HttpStatusCode.OK, (await holding).StatusCode);
            gate.Let(1);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/minimal")).StatusCode);
        }
    }

    [Fact]
    public async Task TheArgumentsAreChecked()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            Assert.Throws<ArgumentNullException>(() => ((RouteHandlerBuilder)null!).RequireConcurrencyLimit("x", 1));
            Assert.Throws<ArgumentException>(() => app.MapGet("/y", () => "y").RequireConcurrencyLimit(" ", 1));
            ConcurrencyLimitAttribute attribute = new("x", 1) { LeaseSeconds = 5 };
            Assert.Equal(("x", 1, 5), (attribute.Name, attribute.Max, attribute.LeaseSeconds));
            await Assert.ThrowsAsync<ArgumentNullException>(() => attribute.OnResourceExecutionAsync(null!, null!));
        }
    }

    [Fact]
    public async Task WithHandOff_ThePlaceIsNotGivenBackWithTheAnswer_TheOneWhoGetsItsHolderGivesItBack()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            HandoffBox box = app.Services.GetRequiredService<HandoffBox>();

            HttpResponseMessage first = await client.GetAsync("/api/hand-off");
            ConcurrencyHandoff handoff = box.Taken!;
            HttpResponseMessage whileHeld = await client.GetAsync("/api/hand-off");   // the answer was written, and the place is still held: the job is still working

            Assert.Equal((HttpStatusCode.OK, HttpStatusCode.TooManyRequests), (first.StatusCode, whileHeld.StatusCode));
            Assert.Equal("hand", handoff.Name);

            // the handler of the event ends: it gives the place back by the holder
            await app.Services.GetRequiredService<IConcurrencyLimiter>().Attach(handoff.Name, handoff.Holder).DisposeAsync();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/hand-off")).StatusCode);
        }
    }

    [Fact]
    public async Task WithHandOff_WhenNoEventTookThePlace_ItIsGivenBackWithTheAnswer()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/hand-off-nothing")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/hand-off-nothing")).StatusCode);   // nothing was handed over: nothing is held
        }
    }

    [Fact]
    public async Task WithHandOff_WhenTheActionFailsAfterTheEventTookThePlace_ItIsGivenBack_ForTheEventMayNotHaveLeft()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();

            // the test server lets the failure of the action through (it has no error page): a 429 would be an answer, not an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/api/hand-off-fails"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/api/hand-off-fails"));   // the second ran: the first did not keep the place
            Assert.NotNull(app.Services.GetRequiredService<HandoffBox>().Taken);
        }
    }

    [Fact]
    public async Task WithHandOff_AMinimalApiEndpointDoesTheSame()
    {
        (WebApplication app, _) = await StartAsync();
        await using (app)
        {
            HttpClient client = app.GetTestClient();
            HandoffBox box = app.Services.GetRequiredService<HandoffBox>();

            HttpResponseMessage first = await client.GetAsync("/minimal-hand-off");
            ConcurrencyHandoff handoff = box.Taken!;
            HttpResponseMessage whileHeld = await client.GetAsync("/minimal-hand-off");
            await app.Services.GetRequiredService<IConcurrencyLimiter>().Attach(handoff.Name, handoff.Holder).DisposeAsync();

            Assert.Equal((HttpStatusCode.OK, HttpStatusCode.TooManyRequests), (first.StatusCode, whileHeld.StatusCode));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/minimal-hand-off")).StatusCode);
        }
    }
}
