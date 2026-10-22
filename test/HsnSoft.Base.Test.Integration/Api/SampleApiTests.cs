using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HsnSoft.Base.Test.Integration.Api;

[Trait("category", "integration")]
[Collection(SampleApiCollection.Name)]
public class SampleApiTests(SampleApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Theory]
    [InlineData("EfCoreUsers")]
    [InlineData("MongoUsers")]
    public async Task GetUserList_ReturnsSeededUsers_InUnifiedResponse(string controller)
    {
        var response = await _client.PostAsJsonAsync($"/api/{controller}/get-user-list", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadAsync(response);
        body.GetProperty("statusCode").GetInt32().Should().Be(200);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("payload").GetArrayLength().Should().BeGreaterThanOrEqualTo(5);
    }

    [Theory]
    [InlineData("EfCoreUsers")]
    [InlineData("MongoUsers")]
    public async Task GetUser_WhenMissing_ReturnsUnifiedNotFound(string controller)
    {
        var response = await _client.GetAsync($"/api/{controller}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ReadAsync(response);
        body.GetProperty("statusCode").GetInt32().Should().Be(404);
        body.GetProperty("statusMessages").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsUnifiedErrorBody()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ReadAsync(response);
        body.GetProperty("errorCode").GetString().Should().Be("http_404");
    }

    [Fact]
    public async Task InvalidBody_ReturnsValidationError()
    {
        var response = await _client.PostAsync("/api/EfCoreUsers/get-paging-users",
            new StringContent("{\"pageNumber\":\"not-a-number\"}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ReadAsync(response);
        body.GetProperty("errorCode").GetString().Should().Be("validation_error");
    }
}
