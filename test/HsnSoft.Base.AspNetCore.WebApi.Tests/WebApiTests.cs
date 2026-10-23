using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

public enum Color
{
    Red,
}

public sealed class Greeting
{
    [Required(ErrorMessage = "Name is required.")]
    public string? Name { get; set; }

    [Range(1, 10)]
    public int Times { get; set; } = 1;

    public string? Note { get; set; }

    public Color Color { get; set; }
}

[ApiController]
[Route("api/answers")]
public sealed class AnswersController : ControllerBase
{
    [HttpGet("object")]
    public IActionResult AnObject() => Ok(new Greeting { Name = "hi", Color = Color.Red });

    [HttpGet("created")]
    public IActionResult CreatedAnswer() => StatusCode(StatusCodes.Status201Created, new { id = 7 });

    [HttpGet("nothing")]
    public IActionResult Nothing() => Ok();

    [HttpGet("json")]
    public IActionResult AsJson() => new JsonResult(new { a = 1 });

    [HttpGet("json-envelope")]
    public IActionResult AsJsonEnvelope() => new JsonResult(new BaseResponse { StatusCode = 200, StatusMessages = ["mine"] });

    [HttpGet("status")]
    public IActionResult OnlyAStatus() => StatusCode(StatusCodes.Status409Conflict);

    [HttpGet("empty")]
    public IActionResult EmptyAnswer() => new EmptyResult();

    [HttpGet("envelope")]
    public IActionResult AlreadyAnEnvelope() => Ok(new BaseResponse<int> { StatusCode = 200, StatusMessages = ["mine"], Payload = 3 });

    [HttpGet("problem")]
    public IActionResult AProblem() => Problem(statusCode: 418, title: "teapot");

    [HttpGet("file")]
    public IActionResult AFile() => File(Encoding.UTF8.GetBytes("bytes"), "text/plain", "a.txt");

    [HttpGet("redirect")]
    public IActionResult Elsewhere() => Redirect("/api/answers/object");

    [HttpPost("greet")]
    public IActionResult Greet(Greeting greeting) => Ok(greeting);

    [HttpGet("business")]
    public IActionResult Business() => throw new BusinessException("order_closed", "The order is closed.").WithData("OrderId", 7);

    [HttpGet("domain")]
    public IActionResult Domain() => throw new DomainException("the rule");

    [HttpGet("http")]
    public IActionResult Http() => throw new BaseHttpException(StatusCodes.Status418ImATeapot, "short and stout");

    [HttpGet("argument")]
    public IActionResult Argument() => throw new ArgumentException("bad argument");

    [HttpGet("denied")]
    public IActionResult Denied() => throw new UnauthorizedAccessException();

    [HttpGet("not-found")]
    public IActionResult Missing() => throw new NotFoundException("order 7 is missing");

    [HttpGet("rule")]
    public IActionResult Rule() => throw new BusinessRuleException("stock is empty");

    [HttpGet("boom")]
    public IActionResult Boom() => throw new InvalidOperationException("secret internals", new InvalidOperationException("inner detail"));

    [HttpGet("started")]
    public async Task Started()
    {
        await Response.WriteAsync("partial");
        throw new InvalidOperationException("after the first byte");
    }
}

public class WebApiTests
{
    private static readonly JsonSerializerOptions s_web = new(JsonSerializerDefaults.Web);

    private static async Task<WebApplication> StartAsync(string environment = "Production", Action<IServiceCollection>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        configure?.Invoke(builder.Services);
        builder.Services.AddBaseWebApi().AddBaseOpenApi();
        builder.Services.AddControllers().AddApplicationPart(typeof(AnswersController).Assembly);

        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.MapBaseOpenApi();
        app.MapControllers();
        app.MapGet("/minimal-color", () => new { color = Color.Red });
        app.MapGet("/binary", context =>
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "image/png";
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(WebApplication app, string path)
    {
        HttpResponseMessage response = await app.GetTestClient().GetAsync(path);
        string text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    [Fact]
    public async Task AnObject_IsThePayloadOfTheOneResponseType()
    {
        await using WebApplication app = await StartAsync();

        (HttpStatusCode status, JsonElement body) = await GetAsync(app, "/api/answers/object");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(200, body.GetProperty("statusCode").GetInt32());
        Assert.Equal("Success", body.GetProperty("statusMessages")[0].GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
        Assert.Equal("hi", body.GetProperty("payload").GetProperty("name").GetString());
        Assert.Equal("Red", body.GetProperty("payload").GetProperty("color").GetString());
        Assert.False(body.GetProperty("payload").TryGetProperty("note", out _));
        Assert.False(body.TryGetProperty("errorCode", out _));
    }

    [Theory]
    [InlineData("/api/answers/created", HttpStatusCode.Created, 201, "Created")]
    [InlineData("/api/answers/status", HttpStatusCode.Conflict, 409, "Request failed.")]
    [InlineData("/api/answers/nothing", HttpStatusCode.OK, 200, "Success")]
    [InlineData("/api/answers/json", HttpStatusCode.OK, 200, "Success")]
    public async Task EveryKindOfResult_ArrivesInTheEnvelope(string path, HttpStatusCode expected, int statusCode, string message)
    {
        await using WebApplication app = await StartAsync();

        (HttpStatusCode status, JsonElement body) = await GetAsync(app, path);

        Assert.Equal(expected, status);
        Assert.Equal(statusCode, body.GetProperty("statusCode").GetInt32());
        Assert.Equal(message, body.GetProperty("statusMessages")[0].GetString());
    }

    [Fact]
    public async Task AnEmptyResult_IsNoContent_WithoutABodyTheClientCouldReadButTheEnvelopeSet()
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/api/answers/empty");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/answers/envelope", "mine")]
    [InlineData("/api/answers/json-envelope", "mine")]
    public async Task WhatAlreadyIsAnEnvelope_IsNotWrappedAgain(string path, string message)
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, path);

        Assert.Equal(message, body.GetProperty("statusMessages")[0].GetString());
        Assert.False(body.TryGetProperty("payload", out JsonElement payload) && payload.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task AProblem_AFile_AndARedirect_AreLeftAlone()
    {
        await using WebApplication app = await StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage problem = await client.GetAsync("/api/answers/problem");
        HttpResponseMessage file = await client.GetAsync("/api/answers/file");

        Assert.Equal(418, (int)problem.StatusCode);
        Assert.Equal("teapot", JsonDocument.Parse(await problem.Content.ReadAsStringAsync()).RootElement.GetProperty("title").GetString());
        Assert.Equal("bytes", await file.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AValidationFailure_IsTheEnvelope_WithAnErrorCodeAndTheFieldsOutsideProduction()
    {
        await using WebApplication development = await StartAsync("Development");
        await using WebApplication production = await StartAsync();

        HttpResponseMessage inDevelopment = await development.GetTestClient().PostAsync("/api/answers/greet", new StringContent("""{"times":99}""", Encoding.UTF8, "application/json"));
        HttpResponseMessage inProduction = await production.GetTestClient().PostAsync("/api/answers/greet", new StringContent("""{"times":99}""", Encoding.UTF8, "application/json"));

        JsonElement shown = JsonDocument.Parse(await inDevelopment.Content.ReadAsStringAsync()).RootElement;
        JsonElement hidden = JsonDocument.Parse(await inProduction.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(HttpStatusCode.BadRequest, inDevelopment.StatusCode);
        Assert.Equal("validation_error", shown.GetProperty("errorCode").GetString());
        Assert.Contains(shown.GetProperty("statusMessages").EnumerateArray().Select(m => m.GetString()), m => m == "Name: Name is required.");
        Assert.Equal("Validation failed.", shown.GetProperty("statusMessages")[0].GetString());
        Assert.Single(hidden.GetProperty("statusMessages").EnumerateArray());
        Assert.Equal("validation_error", hidden.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AValidRequest_IsAnswered_AndAStringThatIsMissingIsNotImplicitlyRequired()
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await app.GetTestClient().PostAsync("/api/answers/greet", new StringContent("""{"name":"x","times":2}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/answers/not-found", HttpStatusCode.NotFound, "order 7 is missing", null)]
    [InlineData("/api/answers/rule", HttpStatusCode.BadRequest, "stock is empty", null)]
    [InlineData("/api/answers/domain", HttpStatusCode.BadRequest, "the rule", null)]
    [InlineData("/api/answers/argument", HttpStatusCode.BadRequest, "bad argument", null)]
    [InlineData("/api/answers/http", (HttpStatusCode)418, "short and stout", null)]
    [InlineData("/api/answers/denied", HttpStatusCode.Unauthorized, "Unauthorized.", null)]
    [InlineData("/api/answers/business", HttpStatusCode.BadRequest, "The order is closed.", "order_closed")]
    public async Task ExceptionsAreAnswers_WithTheStatusTheyMean(string path, HttpStatusCode expected, string message, string? errorCode)
    {
        await using WebApplication app = await StartAsync();

        (HttpStatusCode status, JsonElement body) = await GetAsync(app, path);

        Assert.Equal(expected, status);
        Assert.Equal((int)expected, body.GetProperty("statusCode").GetInt32());
        Assert.Equal(message, body.GetProperty("statusMessages")[0].GetString());
        Assert.Equal(errorCode, body.TryGetProperty("errorCode", out JsonElement code) ? code.GetString() : null);
    }

    [Fact]
    public async Task ABusinessException_SaysItsCodeAndItsData()
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, "/api/answers/business");

        string?[] messages = [.. body.GetProperty("statusMessages").EnumerateArray().Select(m => m.GetString())];
        Assert.Contains("Error code: order_closed", messages);
        Assert.Contains("OrderId: 7", messages);
    }

    [Fact]
    public async Task UnexpectedExceptions_Become500_WithoutLeakingTheMessage_OrTheInnerOnesInProduction()
    {
        await using WebApplication production = await StartAsync();
        await using WebApplication development = await StartAsync("Development");

        (HttpStatusCode status, JsonElement hidden) = await GetAsync(production, "/api/answers/boom");
        (_, JsonElement shown) = await GetAsync(development, "/api/answers/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("Internal server error.", hidden.GetProperty("statusMessages")[0].GetString());
        Assert.Single(hidden.GetProperty("statusMessages").EnumerateArray());
        Assert.DoesNotContain("secret", hidden.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("inner detail", shown.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("secret", shown.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExceptionAfterTheFirstByte_CannotBeAnswered_AndThrowsToTheServer()
    {
        await using WebApplication app = await StartAsync();

        await Assert.ThrowsAnyAsync<Exception>(async () => await (await app.GetTestClient().GetAsync("/api/answers/started", HttpCompletionOption.ResponseHeadersRead)).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownRoute_GetsTheEnvelope_AndABinaryAnswerDoesNot()
    {
        await using WebApplication app = await StartAsync();

        (HttpStatusCode status, JsonElement body) = await GetAsync(app, "/nowhere");
        HttpResponseMessage binary = await app.GetTestClient().GetAsync("/binary");

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal("http_404", body.GetProperty("errorCode").GetString());
        Assert.Equal("Resource not found.", body.GetProperty("statusMessages")[0].GetString());
        Assert.Equal(string.Empty, await binary.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Enums_AreWrittenAsStrings_ForMinimalApisToo()
    {
        await using WebApplication app = await StartAsync();

        string json = await app.GetTestClient().GetStringAsync("/minimal-color");

        Assert.Equal("Red", JsonDocument.Parse(json).RootElement.GetProperty("color").GetString());
    }

    [Fact]
    public async Task TheTextsAndTheMapping_CanBeReplaced()
    {
        await using WebApplication app = await StartAsync(configure: services =>
        {
            services.AddSingleton<IStatusMessageProvider, TurkishMessages>();
        });

        (_, JsonElement body) = await GetAsync(app, "/api/answers/object");

        Assert.Equal("Başarılı", body.GetProperty("statusMessages")[0].GetString());
    }

    private sealed class StartedResponse : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class TurkishMessages : IStatusMessageProvider
    {
        public string GetMessage(int statusCode) => "Başarılı";
    }

    [Fact]
    public async Task OpenApiDocument_IsServedInDevelopmentOnly()
    {
        await using WebApplication development = await StartAsync("Development");
        await using WebApplication production = await StartAsync();

        Assert.Equal(HttpStatusCode.OK, (await development.GetTestClient().GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await production.GetTestClient().GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task TheWriter_WritesErrorsAndSuccessesInTheEnvelope_ButNothingOnceTheResponseStarted()
    {
        ApiResponseWriter writer = new();
        DefaultHttpContext error = new() { TraceIdentifier = "trace-1" };
        error.Response.Body = new MemoryStream();
        DefaultHttpContext success = new();
        success.Response.Body = new MemoryStream();
        DefaultHttpContext started = new();
        started.Features.Set<IHttpResponseFeature>(new StartedResponse());
        started.Response.Body = new MemoryStream();

        await writer.WriteErrorAsync(error, 418, ["a", "a", "b"], "code");
        await writer.WriteSuccessAsync(success, 200, new { x = 1 }, ["ok"]);
        await writer.WriteErrorAsync(started, 500, ["late"]);
        await writer.WriteSuccessAsync(started, 200, 1, ["late"]);

        error.Response.Body.Position = 0;
        BaseResponse? written = await JsonSerializer.DeserializeAsync<BaseResponse>(error.Response.Body, s_web);
        Assert.Equal(418, error.Response.StatusCode);
        Assert.Equal(["a", "b"], written!.StatusMessages);
        Assert.Equal("trace-1", written.TraceId);
        Assert.Equal("code", written.ErrorCode);
        Assert.StartsWith("application/json", error.Response.ContentType, StringComparison.Ordinal);
        Assert.Equal(200, success.Response.StatusCode);
        Assert.Equal(0, started.Response.Body.Length);
        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteErrorAsync(null!, 400, []));
        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteErrorAsync(error, 400, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteSuccessAsync<int>(null!, 200, 1, []));
        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteSuccessAsync<int>(error, 200, 1, null!));
    }

    [Theory]
    [InlineData(200, "Success")]
    [InlineData(201, "Created")]
    [InlineData(202, "Accepted")]
    [InlineData(204, "No content")]
    [InlineData(400, "Bad request.")]
    [InlineData(401, "Unauthorized.")]
    [InlineData(403, "Forbidden.")]
    [InlineData(404, "Resource not found.")]
    [InlineData(405, "Method not allowed.")]
    [InlineData(408, "Request timeout.")]
    [InlineData(415, "Unsupported media type.")]
    [InlineData(429, "Too many requests.")]
    [InlineData(500, "Internal server error.")]
    [InlineData(502, "Bad gateway.")]
    [InlineData(503, "Service unavailable.")]
    [InlineData(504, "Gateway timeout.")]
    [InlineData(418, "Request failed.")]
    public void StatusMessages_AreThereForTheCodesServicesAnswerWith(int code, string message)
        => Assert.Equal(message, new StatusMessageProvider().GetMessage(code));

    [Fact]
    public async Task AnExceptionAfterTheResponseStarted_IsNotAnswered_AndTheHandlerSaysSo()
    {
        await using WebApplication app = await StartAsync();
        GlobalExceptionHandler handler = new(
            app.Services.GetRequiredService<IApiExceptionMapper>(),
            app.Services.GetRequiredService<IApiResponseWriter>(),
            app.Services.GetRequiredService<IHostEnvironment>(),
            NullLogger<GlobalExceptionHandler>.Instance);
        DefaultHttpContext started = new();
        started.Features.Set<IHttpResponseFeature>(new StartedResponse());
        started.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(started, new InvalidOperationException("late"), default);

        Assert.False(handled);
        Assert.Equal(0, started.Response.Body.Length);
    }

    [Fact]
    public async Task HandlerRejectsNullArguments()
    {
        await using WebApplication app = await StartAsync();
        GlobalExceptionHandler handler = new(
            app.Services.GetRequiredService<IApiExceptionMapper>(),
            app.Services.GetRequiredService<IApiResponseWriter>(),
            app.Services.GetRequiredService<IHostEnvironment>(),
            NullLogger<GlobalExceptionHandler>.Instance);
        ApiExceptionMapper mapper = new(new StatusMessageProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.TryHandleAsync(null!, new InvalidOperationException(), default));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.TryHandleAsync(new DefaultHttpContext(), null!, default));
        Assert.Throws<ArgumentNullException>(() => mapper.Map(null!, app.Services.GetRequiredService<IHostEnvironment>()));
        Assert.Throws<ArgumentNullException>(() => mapper.Map(new InvalidOperationException(), null!));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WebApiServiceCollectionExtensions.AddBaseWebApi(null!));
        Assert.Throws<ArgumentNullException>(() => WebApiServiceCollectionExtensions.UseBaseExceptionHandling(null!));
        Assert.Throws<ArgumentNullException>(() => OpenApiExtensions.AddBaseOpenApi(null!));
        Assert.Throws<ArgumentNullException>(() => OpenApiExtensions.MapBaseOpenApi(null!));
    }
}
