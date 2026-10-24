using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.SwaggerGateway.Tests;

public class SwaggerGatewayTests
{
    private const string OrdersDocument = """{"openapi":"3.0.1","info":{"title":"Orders"},"servers":[{"url":"http://orders:8080"}],"paths":{"/api/orders":{"get":{}},"/api/orders/{id}":{"get":{}},"/health":{"get":{}}}}""";

    private sealed class Services : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        public Queue<Func<HttpResponseMessage>> Answers { get; } = new();

        public Func<HttpResponseMessage> Default { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(OrdersDocument) };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(Answers.TryDequeue(out Func<HttpResponseMessage>? next) ? next() : Default());
        }
    }

    private static async Task<WebApplication> StartAsync(Services services, Dictionary<string, string?>? settings = null, string environment = "Development", string? pathBase = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["SwaggerGateway:RetryDelayMilliseconds"] = "1",
            ["SwaggerGateway:Services:0:Key"] = "orders",
            ["SwaggerGateway:Services:0:DisplayName"] = "Orders API",
            ["SwaggerGateway:Services:0:Origin"] = "http://orders:8080",
            ["SwaggerGateway:Services:1:Key"] = "products",
            ["SwaggerGateway:Services:1:Origin"] = "http://products:8080",
        };
        foreach (KeyValuePair<string, string?> setting in settings ?? [])
        {
            values[setting.Key] = setting.Value;
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddBaseSwagger("Gateway API", builder.Configuration, name: "gateway");
        builder.Services.AddBaseSwaggerGateway(builder.Configuration);
        builder.Services.AddHttpClient("HsnSoft.Base.AspNetCore.SwaggerGateway").ConfigurePrimaryHttpMessageHandler(() => services);

        WebApplication app = builder.Build();
        if (pathBase is not null)
        {
            app.UsePathBase(pathBase);
        }

        app.UseBaseSwagger();
        await app.StartAsync();
        return app;
    }

    private static Task<HttpResponseMessage> GetAsync(WebApplication app, string key = "orders")
        => app.GetTestClient().GetAsync($"/swagger/services/{key}/swagger.json");

    [Fact]
    public async Task TheUi_ListsTheGatewayAndEveryService()
    {
        await using WebApplication app = await StartAsync(new Services());

        string ui = await app.GetTestClient().GetStringAsync("/swagger/index.js");

        Assert.Contains("/swagger/gateway/swagger.json", ui, StringComparison.Ordinal);
        Assert.Contains("/swagger/services/orders/swagger.json", ui, StringComparison.Ordinal);
        Assert.Contains("Orders API", ui, StringComparison.Ordinal);
        // a service without a display name is shown by its key
        Assert.Contains("/swagger/services/products/swagger.json", ui, StringComparison.Ordinal);
        Assert.Contains("\"products\"", ui, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheDocumentOfAService_IsFetchedFromItsInternalAddress_AndShownWithTheGatewaysAddress()
    {
        Services services = new();
        await using WebApplication app = await StartAsync(services);

        HttpResponseMessage response = await GetAsync(app);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Uri("http://orders:8080/swagger/v1/swagger.json"), Assert.Single(services.Requests));
        JsonElement document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("http://localhost", document.GetProperty("servers")[0].GetProperty("url").GetString());
        Assert.Equal("Orders", document.GetProperty("info").GetProperty("title").GetString());
    }

    [Fact]
    public async Task BehindAPathBase_TheAddressOfTheGatewayHasIt()
    {
        await using WebApplication app = await StartAsync(new Services(), pathBase: "/gw");

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/gw/swagger/services/orders/swagger.json");

        JsonElement document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("http://localhost/gw", document.GetProperty("servers")[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task ThePublicOrigin_IsWhatTheDocumentsSay_WhenItIsGiven()
    {
        await using WebApplication app = await StartAsync(new Services(), new() { ["SwaggerGateway:PublicOrigin"] = "https://api.example.com/" });

        JsonElement document = JsonDocument.Parse(await (await GetAsync(app)).Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("https://api.example.com", document.GetProperty("servers")[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task ThePathsOfAService_StartAsTheGatewayHasThem()
    {
        await using WebApplication app = await StartAsync(new Services(), new()
        {
            ["SwaggerGateway:Services:0:DownstreamPathPrefix"] = "/api/orders",
            ["SwaggerGateway:Services:0:GatewayPathPrefix"] = "/orders/v1",
        });

        JsonElement paths = JsonDocument.Parse(await (await GetAsync(app)).Content.ReadAsStringAsync()).RootElement.GetProperty("paths");

        // /health is not under /api/orders: this gateway does not forward it, so its document does not show it
        Assert.Equal(["/orders/v1", "/orders/v1/{id}"], paths.EnumerateObject().Select(path => path.Name));
    }

    [Fact]
    public async Task AGatewayPrefix_IsAddedWhenTheServiceHasNone()
    {
        await using WebApplication app = await StartAsync(new Services(), new() { ["SwaggerGateway:Services:0:GatewayPathPrefix"] = "/orders" });

        JsonElement paths = JsonDocument.Parse(await (await GetAsync(app)).Content.ReadAsStringAsync()).RootElement.GetProperty("paths");

        Assert.Equal(["/orders/api/orders", "/orders/api/orders/{id}", "/orders/health"], paths.EnumerateObject().Select(path => path.Name));
    }

    [Fact]
    public async Task ADocument_IsKeptForAWhile_SoTheServiceIsNotCalledEveryTime()
    {
        Services services = new();
        await using WebApplication app = await StartAsync(services);

        await GetAsync(app);
        await GetAsync(app);

        Assert.Single(services.Requests);
    }

    [Fact]
    public async Task ATemporaryFailure_IsTriedAgain_ThenTheDocumentIsServed()
    {
        Services services = new();
        services.Answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        services.Answers.Enqueue(() => throw new HttpRequestException("connection refused"));
        await using WebApplication app = await StartAsync(services);

        HttpResponseMessage response = await GetAsync(app);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, services.Requests.Count);
    }

    [Fact]
    public async Task AServiceThatKeepsFailing_IsAnswered502WithItsName_AfterAllTheTries()
    {
        Services services = new() { Default = () => new HttpResponseMessage(HttpStatusCode.BadGateway) };
        await using WebApplication app = await StartAsync(services);

        HttpResponseMessage response = await GetAsync(app);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("orders", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(3, services.Requests.Count);
    }

    [Fact]
    public async Task APermanentFailure_IsNotTriedAgain()
    {
        Services services = new() { Default = () => new HttpResponseMessage(HttpStatusCode.NotFound) };
        await using WebApplication app = await StartAsync(services);

        Assert.Equal(HttpStatusCode.BadGateway, (await GetAsync(app)).StatusCode);
        Assert.Single(services.Requests);
    }

    [Fact]
    public async Task ADocumentThatIsNotJson_IsA502Too()
    {
        Services services = new() { Default = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>login</html>") } };
        await using WebApplication app = await StartAsync(services);

        Assert.Equal(HttpStatusCode.BadGateway, (await GetAsync(app)).StatusCode);
    }

    [Theory]
    [InlineData("/swagger/services/unknown/swagger.json")]
    [InlineData("/swagger/services/orders")]
    [InlineData("/swagger/services/orders/swagger.json/extra")]
    public async Task AnythingElseUnderTheServicesPath_IsNotFound(string path)
    {
        await using WebApplication app = await StartAsync(new Services());

        Assert.Equal(HttpStatusCode.NotFound, (await app.GetTestClient().GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task OnlyGetServesADocument()
    {
        await using WebApplication app = await StartAsync(new Services());

        Assert.Equal(HttpStatusCode.NotFound, (await app.GetTestClient().PostAsync("/swagger/services/orders/swagger.json", null)).StatusCode);
    }

    [Fact]
    public async Task WhereTheServiceRunsForReal_NothingIsServed_AndNoServiceIsCalled()
    {
        Services services = new();
        await using WebApplication app = await StartAsync(services, environment: "Production");

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(app)).StatusCode);
        Assert.Empty(services.Requests);
    }

    [Fact]
    public void TheSettings_AreChecked()
    {
        SwaggerGatewayOptionsValidator validator = new();
        static SwaggerServiceDefinition Good(string key = "orders") => new() { Key = key, Origin = "http://orders:8080" };

        Assert.True(validator.Validate(null, new SwaggerGatewayOptions { Services = [Good(), Good("products")] }).Succeeded);
        Assert.True(validator.Validate(null, new SwaggerGatewayOptions()).Succeeded);

        foreach (SwaggerGatewayOptions bad in new[]
                 {
                     new SwaggerGatewayOptions { PublicOrigin = "api.example.com" },
                     new SwaggerGatewayOptions { Services = [Good(), Good("ORDERS")] },
                     new SwaggerGatewayOptions { Services = [Good("not a key")] },
                     new SwaggerGatewayOptions { Services = [new SwaggerServiceDefinition { Key = "orders", Origin = "orders:8080" }] },
                     new SwaggerGatewayOptions { Services = [new SwaggerServiceDefinition { Key = "orders", Origin = "ftp://orders" }] },
                     new SwaggerGatewayOptions { Services = [new SwaggerServiceDefinition { Key = "orders", Origin = "http://orders", SwaggerPath = "swagger.json" }] },
                     new SwaggerGatewayOptions { HttpTimeoutSeconds = 0 },
                     new SwaggerGatewayOptions { RetryCount = -1 },
                 })
        {
            Assert.False(validator.Validate(null, bad).Succeeded);
        }
    }

    [Fact]
    public async Task ASettingThatIsWrong_StopsTheHostAtStart()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["SwaggerGateway:Services:0:Key"] = "orders" });
        builder.Services.AddBaseSwagger("Gateway API", builder.Configuration).AddBaseSwaggerGateway(builder.Configuration);
        await using WebApplication app = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SwaggerGatewayExtensions.AddBaseSwaggerGateway(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseSwaggerGateway(null!));
    }
}
