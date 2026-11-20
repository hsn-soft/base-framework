using System.Net;
using System.Text.Json;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

[ApiController]
[Route("api/repository-failures")]
public sealed class RepositoryFailuresController : ControllerBase
{
    [HttpGet("missing")]
    public IActionResult Missing() => throw RepositoryException.NotFound("Order", [Guid.Parse("11111111-1111-1111-1111-111111111111"), 7]);

    [HttpGet("stale")]
    public IActionResult Stale() => throw RepositoryException.Conflict("Order", ["a", "b"]);

    [HttpGet("duplicate")]
    public IActionResult Duplicate() => throw RepositoryException.Duplicate("User", "ux_user_email");

    [HttpGet("unnamed")]
    public IActionResult Unnamed() => throw RepositoryException.Conflict("Order");
}

/// <summary>What the client reads when a repository write fails: the status, the stable code, the text in the language of the request and the entity and the records that failed.</summary>
public sealed class RepositoryAnswersTests
{
    private static async Task<WebApplication> StartAsync(string environment = "Production")
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi();
        builder.Services.AddBaseLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(RepositoryFailuresController).Assembly);
        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.UseBaseLocalization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> GetAsync(WebApplication app, string path, string language)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Accept-Language", language);
        HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        return (response, JsonElement.Parse(await response.Content.ReadAsStringAsync()).Clone());
    }

    private static string[] Messages(JsonElement body) => [.. body.GetProperty("statusMessages").EnumerateArray().Select(message => message.GetString()!)];

    [Theory]
    [InlineData("en", "Order was not found.")]
    [InlineData("tr", "Order bulunamadı.")]
    [InlineData("ru", "Order не найден.")]
    public async Task ARecordThatIsNotThere_Is404_WithItsCode_ItsTextInTheLanguage_AndTheRecordsItNames(string language, string text)
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/repository-failures/missing", language);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("entity_not_found", body.GetProperty("errorCode").GetString());
        Assert.StartsWith(text, Messages(body)[0], StringComparison.Ordinal);
        Assert.Contains("entity: Order", Messages(body));
        Assert.Contains("ids: 11111111-1111-1111-1111-111111111111, 7", Messages(body));
    }

    [Theory]
    [InlineData("en", "Order was changed or removed by someone else after you read it.")]
    [InlineData("tr", "Order siz okuduktan sonra başkası tarafından değiştirildi veya silindi.")]
    [InlineData("ru", "Order был изменён или удалён другим пользователем после того, как вы его прочитали.")]
    public async Task ARecordThatIsNotCurrent_Is409_WithItsCode_AndItsTextInTheLanguage(string language, string text)
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/repository-failures/stale", language);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("concurrency_conflict", body.GetProperty("errorCode").GetString());
        Assert.StartsWith(text, Messages(body)[0], StringComparison.Ordinal);
        Assert.Contains("ids: a, b", Messages(body));
    }

    [Fact]
    public async Task AFailureThatNamesNoRecord_SaysOnlyTheEntity()
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, "/api/repository-failures/unnamed", "en");

        Assert.Contains("entity: Order", Messages(body));
        Assert.DoesNotContain(Messages(body), message => message.StartsWith("ids:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("en", "Another User already has the same unique value.")]
    [InlineData("tr", "Başka bir User aynı benzersiz değere sahip.")]
    [InlineData("ru", "У другого объекта User уже есть такое же уникальное значение.")]
    public async Task AValueThatAnotherRecordHas_Is409_WithItsCode_ItsTextInTheLanguage_AndTheNameOfTheIndex(string language, string text)
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/repository-failures/duplicate", language);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_key", body.GetProperty("errorCode").GetString());
        Assert.StartsWith(text, Messages(body)[0], StringComparison.Ordinal);
        Assert.Contains("constraint: ux_user_email", Messages(body));
        Assert.Contains("entity: User", Messages(body));
    }
}
