using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.Swashbuckle.Tests;

public class SwaggerTests
{
    public sealed record Reply(string Name);

    private static async Task<WebApplication> StartAsync(
        string environment,
        Action<IServiceCollection, IConfiguration> register,
        Dictionary<string, string?>? settings = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        register(builder.Services, builder.Configuration);

        WebApplication app = builder.Build();
        app.UseBaseSwagger();
        app.MapGet("/ping", () => new Reply("pong"));
        await app.StartAsync();
        return app;
    }

    private static async Task<JsonElement> DocumentAsync(WebApplication app, string name = "v1")
    {
        HttpResponseMessage response = await app.GetTestClient().GetAsync($"/swagger/{name}/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Test", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    [InlineData("Staging", HttpStatusCode.NotFound)]
    [InlineData("stage", HttpStatusCode.NotFound)]
    [InlineData("uat", HttpStatusCode.NotFound)]
    public async Task TheDocumentAndTheUi_AreServedExceptWhereTheServiceRunsForReal(string environment, HttpStatusCode expected)
    {
        await using WebApplication app = await StartAsync(environment, (services, configuration) => services.AddBaseSwagger("Orders API", configuration));

        Assert.Equal(expected, (await app.GetTestClient().GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(expected, (await app.GetTestClient().GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task TheDocument_HasTheTitleTheVersionAndTheEndpoints_AndTheUiPointsAtIt()
    {
        await using WebApplication app = await StartAsync("Development", (services, configuration) => services.AddBaseSwagger("Orders API", configuration, "2.1"));

        JsonElement document = await DocumentAsync(app);

        Assert.Equal("Orders API", document.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("2.1", document.GetProperty("info").GetProperty("version").GetString());
        Assert.True(document.GetProperty("paths").TryGetProperty("/ping", out _));
        // the types are named by their full name, so two types of one name do not collide
        Assert.Contains("HsnSoft.Base.AspNetCore.Swashbuckle.Tests.SwaggerTests+Reply", document.GetProperty("components").GetProperty("schemas").EnumerateObject().Select(schema => schema.Name));
        // the UI reads its settings from index.js: the document it shows and the name it gives it
        string ui = await app.GetTestClient().GetStringAsync("/swagger/index.js");
        Assert.Contains("/swagger/v1/swagger.json", ui, StringComparison.Ordinal);
        Assert.Contains("Orders API", ui, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheDocumentCanHaveAnotherName_ForAGateway()
    {
        await using WebApplication app = await StartAsync("Development", (services, configuration) => services.AddBaseSwagger("Gateway API", configuration, name: "gateway"));

        Assert.Equal("Gateway API", (await DocumentAsync(app, "gateway")).GetProperty("info").GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await app.GetTestClient().GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task TheBearerButton_SendsTheTokenInTheAuthorizationHeader()
    {
        await using WebApplication app = await StartAsync("Development", (services, configuration) => services.AddBaseSwaggerWithBearer("Orders API", "Enter a token", configuration));

        JsonElement scheme = (await DocumentAsync(app)).GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString(), ignoreCase: true);
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());
        Assert.Equal("Enter a token", scheme.GetProperty("description").GetString());
    }

    [Fact]
    public async Task TheOAuthButton_UsesTheAuthorizationCodeFlowOfTheAuthority()
    {
        await using WebApplication app = await StartAsync(
            "Development",
            (services, configuration) => services.AddBaseSwaggerWithOAuth("Orders API", "https://identity.example.com", new Dictionary<string, string> { ["orders"] = "Orders" }, configuration));

        JsonElement flow = (await DocumentAsync(app)).GetProperty("components").GetProperty("securitySchemes").GetProperty("oauth2").GetProperty("flows").GetProperty("authorizationCode");

        Assert.Equal("https://identity.example.com/connect/authorize", flow.GetProperty("authorizationUrl").GetString());
        Assert.Equal("https://identity.example.com/connect/token", flow.GetProperty("tokenUrl").GetString());
        Assert.Equal("Orders", flow.GetProperty("scopes").GetProperty("orders").GetString());
    }

    [Fact]
    public async Task TheApplication_CanAddWhatSwashbuckleCanDo()
    {
        await using WebApplication app = await StartAsync(
            "Development",
            (services, configuration) => services.AddBaseSwagger("Orders API", configuration, configure: options => options.SwaggerGeneratorOptions.SwaggerDocs["v1"].Description = "About orders"));

        Assert.Equal("About orders", (await DocumentAsync(app)).GetProperty("info").GetProperty("description").GetString());
    }

    [Fact]
    public async Task TheConfiguration_DecidesWhereItIsServed_AndWhere()
    {
        await using WebApplication on = await StartAsync("Production", (services, configuration) => services.AddBaseSwagger("A", configuration), new() { ["Swagger:Enabled"] = "true", ["Swagger:RoutePrefix"] = "docs" });
        await using WebApplication off = await StartAsync("Development", (services, configuration) => services.AddBaseSwagger("A", configuration), new() { ["Swagger:Enabled"] = "false" });
        await using WebApplication custom = await StartAsync("Test", (services, configuration) => services.AddBaseSwagger("A", configuration), new() { ["Swagger:DisabledIn:0"] = "Test" });

        Assert.Equal(HttpStatusCode.OK, (await on.GetTestClient().GetAsync("/docs/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await off.GetTestClient().GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await custom.GetTestClient().GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task WithoutTheServices_UsingItSaysWhatIsMissing()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using WebApplication app = builder.Build();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => app.UseBaseSwagger());

        Assert.Contains("AddBaseSwagger", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BadArguments_AreRejected()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() => SwaggerServiceCollectionExtensions.AddBaseSwagger(null!, "A"));
        Assert.Throws<ArgumentException>(() => services.AddBaseSwagger(" "));
        Assert.Throws<ArgumentException>(() => services.AddBaseSwagger("A", version: ""));
        Assert.Throws<ArgumentException>(() => services.AddBaseSwagger("A", name: ""));
        Assert.Throws<ArgumentException>(() => services.AddBaseSwaggerWithBearer("A", " "));
        Assert.Throws<ArgumentException>(() => services.AddBaseSwaggerWithOAuth("A", " ", new Dictionary<string, string>()));
        Assert.Throws<ArgumentNullException>(() => services.AddBaseSwaggerWithOAuth("A", "https://a", null!));
        Assert.Throws<ArgumentNullException>(() => SwaggerServiceCollectionExtensions.UseBaseSwagger(null!));
    }
}
