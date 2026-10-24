using System.Net;
using System.Security.Claims;
using System.Text;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

[ApiController]
[Route("api/[controller]")]
public sealed class TokenController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login() => Ok(new { token = "abc" });
}

public sealed record ShapedRequest(string Name, string Secret);

public sealed record ShapedResponse(string Id, string Token);

[ApiController]
[Route("api/shaped")]
public sealed class ShapedController : ControllerBase
{
    [HttpPost]
    public IActionResult Post(ShapedRequest request) => Ok(new ShapedResponse("1", request.Secret));

    [HttpGet("nothing")]
    public IActionResult Nothing() => Ok();
}

public sealed class RequestLoggingTests
{
    private sealed record Logged(LogLevel Level, string? EventName, string Message, Exception? Exception, IReadOnlyDictionary<string, object?> Properties);

    private sealed class CaptureProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

        public List<Logged> Logs { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class CaptureLogger(CaptureProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => owner._scopes.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Dictionary<string, object?> properties = [];
                owner._scopes.ForEachScope(
                    (scope, target) =>
                    {
                        if (scope is IEnumerable<KeyValuePair<string, object?>> values)
                        {
                            foreach (KeyValuePair<string, object?> pair in values)
                            {
                                target[pair.Key] = pair.Value;
                            }
                        }
                    },
                    properties);
                if (state is IEnumerable<KeyValuePair<string, object?>> own)
                {
                    foreach (KeyValuePair<string, object?> pair in own)
                    {
                        properties[pair.Key] = pair.Value;
                    }
                }

                lock (owner.Logs)
                {
                    owner.Logs.Add(new Logged(logLevel, eventId.Name, formatter(state, exception), exception, properties));
                }
            }
        }
    }

    private sealed class MaskingStub : ILogMasker
    {
        public List<Type?> Shapes { get; } = [];

        public string MaskText(string? text) => (text ?? string.Empty).Replace("hunter2", "*****", StringComparison.Ordinal);

        public string MaskText(string? text, Type? shape)
        {
            lock (Shapes)
            {
                Shapes.Add(shape);
            }

            return MaskText(text);
        }

        public object MaskObject<T>(T value) => value!;
    }

    private sealed class Rig(WebApplication app, CaptureProvider logs) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();

        public MaskingStub Masker => (MaskingStub)app.Services.GetRequiredService<ILogMasker>();

        public IReadOnlyList<Logged> Http
        {
            get
            {
                lock (logs.Logs)
                {
                    return [.. logs.Logs.Where(log => log.EventName is "HTTP_REQUEST" or "HTTP_RESPONSE")];
                }
            }
        }

        public IReadOnlyList<Logged> Combined
        {
            get
            {
                lock (logs.Logs)
                {
                    return [.. logs.Logs.Where(log => log.EventName == "HTTP_REQUEST_RESPONSE")];
                }
            }
        }

        public Logged Request => Assert.Single(Http, log => log.EventName == "HTTP_REQUEST");

        public Logged Response => Assert.Single(Http, log => log.EventName == "HTTP_RESPONSE");

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }

    private static async Task<Rig> StartAsync(
        Dictionary<string, string?>? settings = null,
        bool masker = true,
        bool exceptionHandler = true,
        Action<WebApplication>? map = null,
        ClaimsPrincipal? user = null,
        Action<IServiceCollection>? configure = null,
        Func<HttpContext, Task>? beforeLogging = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        CaptureProvider logs = new();
        builder.Logging.ClearProviders().AddProvider(logs);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        builder.Services.AddBaseRequestLogging(configuration).AddBaseWebApi();
        configure?.Invoke(builder.Services);
        builder.Services.AddControllers().AddApplicationPart(typeof(TokenController).Assembly);
        if (masker)
        {
            builder.Services.AddSingleton<ILogMasker, MaskingStub>();
        }

        WebApplication app = builder.Build();
        if (user is not null)
        {
            app.Use((context, next) =>
            {
                context.User = user;
                return next(context);
            });
        }

        if (beforeLogging is not null)
        {
            app.Use(async (context, next) =>
            {
                await beforeLogging(context);
                await next(context);
            });
        }

        app.UseBaseRequestLogging();
        if (exceptionHandler)
        {
            app.UseBaseExceptionHandling();
        }

        app.MapControllers();
        app.MapGet("/api/orders/{id}", (string id) => Results.Ok(new { id }));
        app.MapGet("/named", () => "named").WithName("Get-Named");
        app.MapGet("/echo-correlation", (HttpRequest request) => request.Headers["X-Correlation-Id"].ToString());
        app.MapMethods("/status/{code:int}", ["GET", "POST"], (int code) => Results.StatusCode(code));
        app.MapGet("/served", (HttpContext context) =>
        {
            // what a service behind a gateway says in every answer it logged
            context.Response.Headers["X-Logged-By"] = "order-service";
            return Results.Ok();
        });
        app.MapPost("/echo-502", async (HttpRequest request) =>
        {
            using MemoryStream copy = new();
            await request.Body.CopyToAsync(copy);
            return Results.Text(Encoding.UTF8.GetString(copy.ToArray()), "application/json", statusCode: 502);
        });
        app.MapGet("/known", () => { throw new BusinessException("order_closed", "The order is closed."); });
        app.MapGet("/boom", () => { throw new InvalidOperationException("boom"); });
        app.MapGet("/missing", () => Results.NotFound());
        app.MapGet("/health/live", () => "ok");
        app.MapGet("/openapi/v1.json", () => "{}");
        app.MapPost("/echo", async (HttpRequest request) => Results.Text(await new StreamReader(request.Body).ReadToEndAsync(), "application/json"));
        app.MapGet("/json", () => Results.Text("""{"password":"hunter2","name":"alice"}""", "application/json"));
        app.MapGet("/text", () => Results.Text("hunter2", "text/plain"));
        app.MapGet("/cookie", (HttpContext context) =>
        {
            context.Response.Headers.SetCookie = "session=abc; path=/; httponly";
            return "ok";
        });
        map?.Invoke(app);
        await app.StartAsync();
        return new Rig(app, logs);
    }

    [Fact]
    public async Task ARequest_IsLoggedAsARequestAndAResponse_ThatShareTheScopeId_AndTheCorrelationId()
    {
        await using Rig rig = await StartAsync();

        HttpResponseMessage response = await rig.Client.GetAsync("/api/orders/7?x=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, rig.Http.Count);
        Logged request = rig.Request;
        Logged answer = rig.Response;
        Assert.Equal("HttpRequest", request.Properties["LogType"]);
        Assert.Equal("HttpResponse", answer.Properties["LogType"]);
        Assert.Equal(request.Properties["ScopeId"], answer.Properties["ScopeId"]);
        Assert.Equal(request.Properties["CorrelationId"], answer.Properties["CorrelationId"]);
        Assert.Equal(request.Properties["CorrelationId"], response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("httpclient -> /api/orders/7: GET /api/orders/7", request.Message);
        Assert.Equal("httpclient", request.Properties["From"]);
        Assert.Equal("/api/orders/7", request.Properties["To"]);
        Assert.Equal("?x=1", request.Properties["Query"]);
        Assert.Equal("200", answer.Properties["Status"]?.ToString());
        Assert.IsType<long>(answer.Properties["ElapsedMs"]);
        Assert.Equal(LogLevel.Information, request.Level);
        Assert.Equal(LogLevel.Information, answer.Level);
    }

    private sealed class ChannelEnricher : IRequestLogEnricher
    {
        public void Enrich(HttpContext context, string logType, IDictionary<string, object?> fields)
            => fields["ClientChannel"] = $"{logType}:{context.Request.Headers["X-Client-Channel"]}";
    }

    [Fact]
    public async Task TheRequestLog_SaysWhoAsked_AndAProductAddsItsOwnFields()
    {
        await using Rig rig = await StartAsync(configure: services => services.AddSingleton<IRequestLogEnricher, ChannelEnricher>());
        HttpRequestMessage request = new(HttpMethod.Get, "/named");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        request.Headers.Add("User-Agent", "TestAgent/1.0");
        request.Headers.Add("Accept-Language", "tr-TR");
        request.Headers.Add("Origin", "https://www.Example.com/");
        request.Headers.Add("Referer", "https://other.example.org/page");
        request.Headers.Add("X-Client-Channel", "mobile");

        await rig.Client.SendAsync(request);

        Logged log = rig.Request;
        Assert.Equal("203.0.113.7", log.Properties["ForwardedFor"]);
        Assert.Equal("TestAgent/1.0", log.Properties["UserAgent"]);
        Assert.Equal("tr-TR", log.Properties["AcceptLanguage"]);
        Assert.Equal("example.com", log.Properties["Origin"]);
        Assert.Equal("other.example.org", log.Properties["Referer"]);
        Assert.Equal("http", log.Properties["Scheme"]);
        Assert.Equal("localhost", log.Properties["Host"]);
        Assert.False(string.IsNullOrEmpty((string?)log.Properties["TraceId"]));
        Assert.Equal("HttpRequest:mobile", log.Properties["ClientChannel"]);
        Assert.Equal("HttpResponse:mobile", rig.Response.Properties["ClientChannel"]);
    }

    [Theory]
    [InlineData("https://www.Example.com/path", "example.com")]
    [InlineData("http://api.example.com", "api.example.com")]
    [InlineData("null", null)]
    [InlineData("https:///", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void AnOriginIsAHost(string? value, string? expected)
        => Assert.Equal(expected, RequestLoggingMiddleware.HostOf(value));

    [Fact]
    public async Task TheOriginIsTheReferer_OrTheFromHeader_WhenThereIsNone()
    {
        await using Rig rig = await StartAsync();
        HttpRequestMessage viaReferer = new(HttpMethod.Get, "/named");
        viaReferer.Headers.Add("Referer", "https://ref.example.com/x");
        HttpRequestMessage viaFrom = new(HttpMethod.Get, "/named");
        viaFrom.Headers.Add("From", "bot@crawler.example.net");

        await rig.Client.SendAsync(viaReferer);
        await rig.Client.SendAsync(viaFrom);

        Assert.Equal(["ref.example.com", "bot@crawler.example.net"], rig.Http.Where(log => log.EventName == "HTTP_REQUEST").Select(log => log.Properties["Origin"]));
    }

    [Fact]
    public async Task AServiceSaysItLoggedTheRequest_AndOneThatDoesNotLogSaysNothing()
    {
        await using Rig logging = await StartAsync();
        await using Rig silent = await StartAsync(new() { ["RequestLogging:Enabled"] = "false" });

        HttpResponseMessage logged = await logging.Client.GetAsync("/named");
        HttpResponseMessage notLogged = await silent.Client.GetAsync("/named");

        Assert.Equal("testhost", logged.Headers.GetValues("X-Logged-By").Single(), ignoreCase: true);
        Assert.False(notLogged.Headers.Contains("X-Logged-By"));
    }

    [Fact]
    public async Task AGateway_LogsNothingForWhatAServiceLogged_AndDoesNotShowTheSignToTheClient()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway" });

        HttpResponseMessage response = await rig.Client.GetAsync("/served");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Logged-By"));
        Assert.Empty(rig.Http);
        Assert.Empty(rig.Combined);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Theory]
    [InlineData("/status/401", LogLevel.Warning)]
    [InlineData("/status/429", LogLevel.Warning)]
    [InlineData("/missing", LogLevel.Warning)]
    [InlineData("/status/502", LogLevel.Error)]
    [InlineData("/status/504", LogLevel.Error)]
    public async Task AGateway_LogsOneRecordForWhatItAnsweredItself_ByItsStatus(string path, LogLevel level)
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway" });

        await rig.Client.GetAsync(path);

        Logged log = Assert.Single(rig.Combined);
        Assert.Empty(rig.Http);
        Assert.Equal(level, log.Level);
        Assert.Equal("HttpRequestResponse", log.Properties["LogType"]);
        Assert.Equal("HTTP_REQUEST_RESPONSE", log.EventName);
        Assert.Equal("httpclient", log.Properties["From"]);
        Assert.Equal(path, log.Properties["To"]);
        Assert.Equal("GET", log.Properties["Method"]);
        Assert.IsType<long>(log.Properties["ElapsedMs"]);
        Assert.NotNull(log.Properties["RequestHeaders"]);
        Assert.NotNull(log.Properties["ResponseHeaders"]);
        Assert.StartsWith($"httpclient -> {path}: GET {path} ", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WhenTheServiceCouldNotBeReached_TheGatewayLogsWhatWasAsked_Masked_AndTheServiceStillReadsIt()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway", ["RequestLogging:LogBodies"] = "true" });

        HttpResponseMessage response = await rig.Client.PostAsync("/echo-502", new StringContent("""{"item":"keyboard","password":"hunter2"}""", Encoding.UTF8, "application/json"));

        Assert.Equal("""{"item":"keyboard","password":"hunter2"}""", await response.Content.ReadAsStringAsync());
        Logged log = Assert.Single(rig.Combined);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Equal("""{"item":"keyboard","password":"*****"}""", log.Properties["RequestBody"]);
        Assert.Equal("""{"item":"keyboard","password":"*****"}""", log.Properties["ResponseBody"]);
    }

    private sealed class BrokenBody : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("the connection broke");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("the connection broke");

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task ARequestThatCannotBeReadAgain_IsLoggedWithoutItsBody_AndTheAnswerStaysWhatItWas()
    {
        await using Rig rig = await StartAsync(
            new() { ["RequestLogging:Role"] = "Gateway", ["RequestLogging:LogBodies"] = "true" },
            beforeLogging: context =>
            {
                context.Request.Body = new BrokenBody();
                return Task.CompletedTask;
            });

        HttpResponseMessage response = await rig.Client.PostAsync("/status/502", new StringContent("""{"a":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Logged log = Assert.Single(rig.Combined);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Null(log.Properties["RequestBody"]);
    }

    [Fact]
    public async Task TheGateway_DoesNotLogTheBodyOfARequestItRejected_OrOneThatIsNotJson_OrTooBig_OrWithoutBodiesOn()
    {
        await using Rig on = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway", ["RequestLogging:LogBodies"] = "true", ["RequestLogging:MaxBodyReadBytes"] = "20" });
        await using Rig off = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway" });
        StringContent json = new("""{"a":1}""", Encoding.UTF8, "application/json");

        await on.Client.PostAsync("/status/401", json);
        await on.Client.PostAsync("/echo-502", new StringContent("plain text", Encoding.UTF8, "text/plain"));
        await on.Client.PostAsync("/echo-502", new StringContent("""{"a":"this is longer than twenty bytes"}""", Encoding.UTF8, "application/json"));
        await off.Client.PostAsync("/echo-502", new StringContent("""{"a":1}""", Encoding.UTF8, "application/json"));

        Assert.All(on.Combined, log => Assert.Null(log.Properties["RequestBody"]));
        Assert.Equal(3, on.Combined.Count);
        Assert.Null(Assert.Single(off.Combined).Properties["RequestBody"]);
    }

    [Fact]
    public async Task AGateway_LogsAnExceptionNobodyHandled_AsAnError_AndStillThrowsIt()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:Role"] = "Gateway" }, exceptionHandler: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => rig.Client.GetAsync("/boom"));

        Logged log = Assert.Single(rig.Combined);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Equal("500", log.Properties["Status"]?.ToString());
    }

    [Fact]
    public async Task AControllerAction_IsNamedControllerDashAction_InLowerCase()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.PostAsync("/api/token/login", null);

        Assert.Equal("token-login", rig.Request.Properties["To"]);
        Assert.Equal("token-login", rig.Response.Properties["To"]);
    }

    [Fact]
    public async Task AnythingButAControllerAction_IsNamedByItsRequestPath_InLowerCase()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync("/Named");
        await rig.Client.GetAsync("/nowhere/At-All");

        Assert.Equal(["/named", "/nowhere/at-all"], rig.Http.Where(log => log.EventName == "HTTP_REQUEST").Select(log => log.Properties["To"]));
    }

    [Fact]
    public async Task TheCorrelationIdOfTheCaller_IsKept_ALongOneIsCut_AndABlankOneIsReplaced()
    {
        await using Rig rig = await StartAsync();

        HttpRequestMessage given = new(HttpMethod.Get, "/named");
        given.Headers.Add("X-Correlation-Id", "chain-1");
        HttpResponseMessage keptResponse = await rig.Client.SendAsync(given);
        HttpRequestMessage tooLong = new(HttpMethod.Get, "/named");
        tooLong.Headers.Add("X-Correlation-Id", new string('x', 500));
        HttpResponseMessage cutResponse = await rig.Client.SendAsync(tooLong);
        HttpRequestMessage blank = new(HttpMethod.Get, "/named");
        blank.Headers.Add("X-Correlation-Id", " ");
        HttpResponseMessage newResponse = await rig.Client.SendAsync(blank);

        Assert.Equal("chain-1", keptResponse.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal(128 + "...(+372)".Length, cutResponse.Headers.GetValues("X-Correlation-Id").Single().Length);
        Assert.Matches("^[0-9a-f]{32}$", newResponse.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Matches("^[0-9a-f]{32}$", (string)rig.Http[^1].Properties["ScopeId"]!);
    }

    [Fact]
    public async Task TheChainIsPutOnTheRequest_SoWhatIsForwardedCarriesIt()
    {
        await using Rig rig = await StartAsync();

        HttpResponseMessage started = await rig.Client.GetAsync("/echo-correlation");
        HttpRequestMessage given = new(HttpMethod.Get, "/echo-correlation");
        given.Headers.Add("X-Correlation-Id", "chain-7");
        HttpResponseMessage continued = await rig.Client.SendAsync(given);

        Assert.Matches("^[0-9a-f]{32}$", await started.Content.ReadAsStringAsync());
        Assert.Equal(started.Headers.GetValues("X-Correlation-Id").Single(), await started.Content.ReadAsStringAsync());
        Assert.Equal("chain-7", await continued.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WhatRunsInTheRequest_KnowsItsChain()
    {
        LogFlowStep? seen = null;
        await using Rig rig = await StartAsync(map: app => app.MapGet("/flow", (ILoggerFactory factory) =>
        {
            seen = LogFlow.Current;
            return factory is null ? "none" : "ok";
        }));

        await rig.Client.GetAsync("/flow");

        Assert.Equal("/flow", seen?.Name);
        Assert.Equal(rig.Request.Properties["ScopeId"], seen?.ScopeId);
        Assert.Equal(rig.Request.Properties["CorrelationId"], seen?.CorrelationId);
        Assert.Null(LogFlow.Current);
    }

    [Theory]
    [InlineData("/missing", LogLevel.Warning)]
    [InlineData("/status/399", LogLevel.Information)]
    [InlineData("/status/400", LogLevel.Warning)]
    [InlineData("/status/499", LogLevel.Warning)]
    [InlineData("/status/500", LogLevel.Error)]
    [InlineData("/status/502", LogLevel.Error)]
    [InlineData("/status/503", LogLevel.Error)]
    [InlineData("/status/504", LogLevel.Error)]
    [InlineData("/api/orders/1", LogLevel.Information)]
    public async Task TheResponseLogLevel_FollowsTheStatus(string path, LogLevel level)
    {
        await using Rig rig = await StartAsync();

        await rig.Client.GetAsync(path);

        Assert.Equal(level, rig.Response.Level);
    }

    [Fact]
    public async Task AFailedRequest_IsAnErrorResponse_WithTheStatusTheClientGets()
    {
        await using Rig rig = await StartAsync();

        HttpResponseMessage response = await rig.Client.GetAsync("/boom");

        // the level is the status: 500 is an Error
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(LogLevel.Error, rig.Response.Level);
        Assert.Equal("500", rig.Response.Properties["Status"]?.ToString());
        Assert.Equal("System.InvalidOperationException", rig.Response.Properties["ExceptionType"]);
        Assert.Null(rig.Response.Properties["ErrorCode"]);
    }

    [Fact]
    public async Task AKnownError_EndsTheRequestAsAWarning_WithItsTypeAndCode()
    {
        await using Rig rig = await StartAsync();

        HttpResponseMessage response = await rig.Client.GetAsync("/known");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(LogLevel.Warning, rig.Response.Level);
        Assert.Equal("HsnSoft.Base.BusinessException", rig.Response.Properties["ExceptionType"]);
        Assert.Equal("order_closed", rig.Response.Properties["ErrorCode"]);
    }

    [Fact]
    public async Task AnExceptionNobodyHandles_IsLoggedAsAnError_AndStillThrown()
    {
        await using Rig rig = await StartAsync(exceptionHandler: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => rig.Client.GetAsync("/boom"));

        Assert.Equal(LogLevel.Error, rig.Response.Level);
        Assert.Equal("boom", rig.Response.Exception?.Message);
        Assert.Equal("500", rig.Response.Properties["Status"]?.ToString());
    }

    [Fact]
    public async Task TheUserOfTheRequest_IsInTheResponseLog()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim("sub", "alice")], "test"));
        ClaimsPrincipal other = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "bob")], "test"));

        await using Rig subject = await StartAsync(user: user);
        await subject.Client.GetAsync("/named");
        await using Rig nameIdentifier = await StartAsync(user: other);
        await nameIdentifier.Client.GetAsync("/named");
        await using Rig anonymous = await StartAsync();
        await anonymous.Client.GetAsync("/named");

        Assert.Equal("alice", subject.Response.Properties["UserId"]);
        Assert.Equal("bob", nameIdentifier.Response.Properties["UserId"]);
        Assert.Null(anonymous.Response.Properties["UserId"]);
    }

    [Fact]
    public async Task HealthChecksAndExcludedPaths_AreNotLogged_ButStillGetACorrelationId()
    {
        await using Rig rig = await StartAsync();

        HttpResponseMessage health = await rig.Client.GetAsync("/health/live");
        await rig.Client.GetAsync("/openapi/v1.json");

        Assert.Empty(rig.Http);
        Assert.True(health.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task HealthChecks_CanBeLogged()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogHealthChecks"] = "true" });

        await rig.Client.GetAsync("/health/live");

        Assert.Equal(2, rig.Http.Count);
    }

    [Fact]
    public async Task WhenDisabled_NothingIsLogged_ButTheChainIsStillSet()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:Enabled"] = "false" });

        HttpResponseMessage response = await rig.Client.GetAsync("/named");

        Assert.Empty(rig.Http);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Headers_AreOneJsonField_WithCredentialsSummarizedAndLongValuesCut()
    {
        await using Rig rig = await StartAsync();

        HttpRequestMessage request = new(HttpMethod.Get, "/cookie");
        request.Headers.Add("Authorization", "Bearer abcdefgh");
        request.Headers.Add("Cookie", "a=1; b=2; a=3");
        request.Headers.Add("X-Long", new string('v', 400));
        await rig.Client.SendAsync(request);
        HttpRequestMessage basic = new(HttpMethod.Get, "/named");
        basic.Headers.Add("Authorization", "Basic dXNlcjpwYXNz");
        basic.Headers.Add("Cookie", "=");
        await rig.Client.SendAsync(basic);

        string first = (string)rig.Http.First(log => log.EventName == "HTTP_REQUEST").Properties["RequestHeaders"]!;
        Assert.Contains("Bearer (length: 8)", first, StringComparison.Ordinal);
        Assert.DoesNotContain("abcdefgh", first, StringComparison.Ordinal);
        Assert.Contains("Cookies: a, b", first, StringComparison.Ordinal);
        Assert.Contains("...(+144)", first, StringComparison.Ordinal);
        Assert.True(first.Length < 800);
        string second = (string)rig.Http.Where(log => log.EventName == "HTTP_REQUEST").ElementAt(1).Properties["RequestHeaders"]!;
        Assert.Contains("AuthHeader (length: 18)", second, StringComparison.Ordinal);
        Assert.Contains("Cookies: <unknown>", second, StringComparison.Ordinal);
        string setCookie = (string)rig.Http.First(log => log.EventName == "HTTP_RESPONSE").Properties["ResponseHeaders"]!;
        Assert.Contains("Set-Cookie: session", setCookie, StringComparison.Ordinal);
        Assert.DoesNotContain("abc;", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bodies_AreNotLogged_ByDefault()
    {
        await using Rig rig = await StartAsync();

        await rig.Client.PostAsync("/echo", new StringContent("""{"password":"hunter2"}""", Encoding.UTF8, "application/json"));

        Assert.Null(rig.Request.Properties["RequestBody"]);
        Assert.Null(rig.Response.Properties["ResponseBody"]);
    }

    [Fact]
    public async Task Bodies_AreLoggedMasked_WhenTurnedOn_AndStillReachTheirReader()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogBodies"] = "true" });

        HttpResponseMessage response = await rig.Client.PostAsync("/echo", new StringContent("""{"password":"hunter2"}""", Encoding.UTF8, "application/json"));

        Assert.Equal("""{"password":"hunter2"}""", await response.Content.ReadAsStringAsync());
        Assert.Equal("""{"password":"*****"}""", rig.Request.Properties["RequestBody"]);
        Assert.Equal("""{"password":"*****"}""", rig.Response.Properties["ResponseBody"]);
    }

    [Fact]
    public async Task TheMasker_IsToldWhichClassTheBodyIs_SoItsAttributesCanBeUsed()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogBodies"] = "true" });

        await rig.Client.PostAsync("/api/shaped", new StringContent("""{"name":"n","secret":"hunter2"}""", Encoding.UTF8, "application/json"));
        await rig.Client.GetAsync("/api/shaped/nothing");

        // the request is the class the action reads its body into; the answer is the class of what the action returned
        Type?[] shapes = [.. rig.Masker.Shapes];
        Assert.Contains(typeof(ShapedRequest), shapes);
        Assert.Contains(typeof(ShapedResponse), shapes);
        Assert.Equal("""{"name":"n","secret":"*****"}""", rig.Http.First(log => log.EventName == "HTTP_REQUEST").Properties["RequestBody"]);
        Assert.Equal(2, shapes.Count(shape => shape is not null));
    }

    [Fact]
    public async Task Bodies_AreCut_AndOnlyJsonOnesAreLogged()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogBodies"] = "true", ["RequestLogging:MaxBodyLength"] = "10" });

        await rig.Client.GetAsync("/json");
        await rig.Client.GetAsync("/text");

        Logged[] responses = [.. rig.Http.Where(log => log.EventName == "HTTP_RESPONSE")];
        Assert.Equal("""{"password...(+25)""", responses[0].Properties["ResponseBody"]);
        Assert.Null(responses[1].Properties["ResponseBody"]);
    }

    [Fact]
    public async Task ABodyThatIsTooBigToRead_IsNotLogged()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogBodies"] = "true", ["RequestLogging:MaxBodyReadBytes"] = "8" });

        HttpResponseMessage response = await rig.Client.PostAsync("/echo", new StringContent("""{"password":"hunter2"}""", Encoding.UTF8, "application/json"));
        HttpResponseMessage empty = await rig.Client.PostAsync("/echo", new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        Assert.Equal("""{"password":"hunter2"}""", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.All(rig.Http.Where(log => log.EventName == "HTTP_REQUEST"), log => Assert.Null(log.Properties["RequestBody"]));
        Assert.All(rig.Http.Where(log => log.EventName == "HTTP_RESPONSE"), log => Assert.Null(log.Properties["ResponseBody"]));
    }

    [Fact]
    public async Task WithoutAMasker_NoBodyIsEverLogged()
    {
        await using Rig rig = await StartAsync(new() { ["RequestLogging:LogBodies"] = "true" }, masker: false);

        HttpResponseMessage response = await rig.Client.PostAsync("/echo", new StringContent("""{"password":"hunter2"}""", Encoding.UTF8, "application/json"));

        Assert.Equal("""{"password":"hunter2"}""", await response.Content.ReadAsStringAsync());
        Assert.Null(rig.Request.Properties["RequestBody"]);
        Assert.Null(rig.Response.Properties["ResponseBody"]);
    }

    [Fact]
    public void TheMiddlewareAndTheOptions_NeedTheirArguments()
    {
        Assert.Throws<ArgumentNullException>(() => RequestLoggingExtensions.AddBaseRequestLogging(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseRequestLogging(null!));
        Assert.Throws<ArgumentNullException>(() => RequestLoggingExtensions.UseBaseRequestLogging(null!));
        RequestLoggingOptions defaults = new();
        Assert.True(defaults.Enabled);
        Assert.False(defaults.LogBodies);
        Assert.Equal("X-Correlation-Id", defaults.CorrelationIdHeader);
    }
}
