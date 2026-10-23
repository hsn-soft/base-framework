using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

public class WebApiTests
{
    private enum Color
    {
        Red,
    }

    private static async Task<WebApplication> StartAsync(string environment = "Production")
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi().AddBaseOpenApi();

        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.MapBaseOpenApi();
        app.MapGet("/not-found", () => { throw new NotFoundException("order 7 is missing"); });
        app.MapGet("/rule", () => { throw new BusinessRuleException("stock is empty"); });
        app.MapGet("/bad-request", () => { throw new BadHttpRequestException("malformed"); });
        app.MapGet("/boom", () => { throw new InvalidOperationException("secret internals"); });
        app.MapGet("/color", () => new { color = Color.Red });
        await app.StartAsync();
        return app;
    }

    [Theory]
    [InlineData("/not-found", HttpStatusCode.NotFound, "Not found", "order 7 is missing")]
    [InlineData("/rule", HttpStatusCode.BadRequest, "Business rule violated", "stock is empty")]
    [InlineData("/bad-request", HttpStatusCode.BadRequest, "Bad request", "malformed")]
    public async Task KnownExceptions_BecomeProblemsWithTheirMessage(string path, HttpStatusCode status, string title, string detail)
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync(path);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(title, problem!.Title);
        Assert.Equal(detail, problem.Detail);
    }

    [Fact]
    public async Task UnexpectedExceptions_Become500_WithoutLeakingTheMessage()
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/boom");
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Unexpected error", problem!.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public async Task UnknownRoute_GetsAProblemBody()
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/nowhere");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Enums_AreWrittenAsStrings()
    {
        await using WebApplication app = await StartAsync();

        string json = await app.GetTestClient().GetStringAsync("/color");

        Assert.Equal("Red", JsonDocument.Parse(json).RootElement.GetProperty("color").GetString());
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
    public async Task HandlerRejectsNullArguments()
    {
        await using WebApplication app = await StartAsync();
        var handler = new GlobalExceptionHandler(app.Services.GetRequiredService<IProblemDetailsService>(), NullLogger<GlobalExceptionHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.TryHandleAsync(null!, new InvalidOperationException(), default));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await handler.TryHandleAsync(new DefaultHttpContext(), null!, default));
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
