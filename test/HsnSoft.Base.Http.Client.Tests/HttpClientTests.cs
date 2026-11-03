using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Http.Client.Tests;

public class HttpClientTests
{
    private sealed class Fake(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string?> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return answer(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpClient ClientOf(Fake fake) => new(fake) { BaseAddress = new Uri("https://gateway.test") };

    public sealed record Item(string Name, Kind Kind);

    public enum Kind
    {
        First,
        Second,
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => pair.Value)).Build();

    // ---------------------------------------------------------------- the answer

    [Fact]
    public async Task APayload_IsReadFromTheBaseResponse_WithTextsForEnums()
    {
        Fake fake = new(_ => Json(HttpStatusCode.OK, """{"statusCode":200,"statusMessages":[],"payload":{"name":"lamp","kind":"Second"}}"""));

        Item? item = await ClientOf(fake).GetPayloadAsync<Item>("/items/1");

        Assert.Equal(new Item("lamp", Kind.Second), item);
        Assert.Equal(HttpMethod.Get, fake.Requests[0].Method);
    }

    [Fact]
    public async Task ABodyIsSentAsCamelCaseJson_WithTextsForEnums_ByPostAndPut()
    {
        Fake fake = new(_ => Json(HttpStatusCode.OK, """{"payload":{"name":"a","kind":"First"}}"""));
        HttpClient client = ClientOf(fake);

        await client.PostPayloadAsync<Item>("/items", new Item("lamp", Kind.Second));
        await client.PutPayloadAsync<Item>("/items", new Item("lamp", Kind.First));

        Assert.Equal("""{"name":"lamp","kind":"Second"}""", fake.Bodies[0]);
        Assert.Equal(HttpMethod.Put, fake.Requests[1].Method);
        Assert.Equal("""{"name":"lamp","kind":"First"}""", fake.Bodies[1]);
        Assert.Equal("application/json", fake.Requests[0].Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task ANoContentAnswer_AndAnAnswerWithoutPayload_AreDefault()
    {
        Fake noContent = new(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        Fake empty = new(_ => Json(HttpStatusCode.OK, """{"statusCode":200}"""));
        Fake unreadable = new(_ => Json(HttpStatusCode.OK, "null"));

        Assert.Null(await ClientOf(noContent).PostPayloadAsync<Item>("/x", null));
        Assert.Null(await ClientOf(empty).GetPayloadAsync<Item>("/x"));
        Assert.Null(await ClientOf(unreadable).GetPayloadAsync<Item>("/x"));
        Assert.Null(noContent.Bodies[0]);
    }

    [Fact]
    public async Task AFailure_IsATypedException_WithTheStatusTheMessagesAndTheErrorCode()
    {
        Fake fake = new(_ => Json(HttpStatusCode.BadRequest, """{"statusCode":400,"statusMessages":["Validation failed.","Name is required."],"errorCode":"validation_error"}"""));

        BaseHttpException exception = await Assert.ThrowsAsync<BaseHttpException>(() => ClientOf(fake).PostPayloadAsync<Item>("/items", new Item("", Kind.First)));

        Assert.Equal(400, exception.HttpStatusCode);
        Assert.Equal("Validation failed., Name is required.", exception.Message);
        Assert.Equal("validation_error", exception.Data["errorCode"]);
    }

    [Fact]
    public async Task AFailureThatIsNotABaseResponse_IsTheStatusAndItsReason()
    {
        Fake html = new(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { ReasonPhrase = "Bad Gateway", Content = new StringContent("<html>proxy</html>", Encoding.UTF8, "text/html") });
        Fake unauthorized = new(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        BaseHttpException badGateway = await Assert.ThrowsAsync<BaseHttpException>(() => ClientOf(html).GetPayloadAsync<Item>("/x"));
        BaseHttpException refused = await Assert.ThrowsAsync<BaseHttpException>(() => ClientOf(unauthorized).GetPayloadAsync<Item>("/x"));

        Assert.Equal((502, "Bad Gateway"), (badGateway.HttpStatusCode, badGateway.Message));
        Assert.Equal(401, refused.HttpStatusCode);
        Assert.False(badGateway.Data.Contains("errorCode"));
    }

    [Fact]
    public async Task ADelete_IsSent_AndAFailureOfItIsTyped()
    {
        Fake ok = new(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        Fake notFound = new(_ => Json(HttpStatusCode.NotFound, """{"statusMessages":["Not found."]}"""));

        await ClientOf(ok).DeleteResourceAsync("/items/1");
        BaseHttpException exception = await Assert.ThrowsAsync<BaseHttpException>(() => ClientOf(notFound).DeleteResourceAsync("/items/1"));

        Assert.Equal(HttpMethod.Delete, ok.Requests[0].Method);
        Assert.Equal(404, exception.HttpStatusCode);
    }

    [Fact]
    public async Task TheArgumentsAreChecked()
    {
        HttpClient client = ClientOf(new Fake(_ => new HttpResponseMessage()));

        await Assert.ThrowsAsync<ArgumentNullException>(() => ((HttpClient)null!).GetPayloadAsync<Item>("/x"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPayloadAsync<Item>(" "));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendPayloadAsync<Item>(null!, "/x", null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((HttpClient)null!).DeleteResourceAsync("/x"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteResourceAsync(""));
    }

    // ---------------------------------------------------------------- the correlation id

    [Fact]
    public async Task TheCorrelationId_OfTheChain_GoesWithTheCall_UnlessTheCallHasOne()
    {
        Fake fake = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient client = new(new CorrelationIdHandler("X-Correlation-Id") { InnerHandler = fake }) { BaseAddress = new Uri("https://gateway.test") };

        using (LogFlow.Begin("chain-1", "scope", "step"))
        {
            await client.GetAsync("/a");
            using HttpRequestMessage own = new(HttpMethod.Get, "/b");
            own.Headers.TryAddWithoutValidation("X-Correlation-Id", "mine");
            await client.SendAsync(own);
        }

        await client.GetAsync("/outside");

        Assert.Equal("chain-1", fake.Requests[0].Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("mine", fake.Requests[1].Headers.GetValues("X-Correlation-Id").Single());
        Assert.False(fake.Requests[2].Headers.Contains("X-Correlation-Id"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new HttpMessageInvoker(new CorrelationIdHandler("h") { InnerHandler = fake }).SendAsync(null!, CancellationToken.None));
    }

    // ---------------------------------------------------------------- the bearer token

    [Fact]
    public async Task TheToken_GoesOnlyToTheHostsItIsFor_AndNeverOverAnotherAuthorization()
    {
        Fake fake = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        string? token = "abc";
        using HttpClient client = new(new BearerTokenRelayHandler(() => token, ["gateway.test"]) { InnerHandler = fake });

        await client.GetAsync("https://gateway.test/a");
        await client.GetAsync("https://GATEWAY.test/b");
        await client.GetAsync("https://evil.test/c");
        using HttpRequestMessage own = new(HttpMethod.Get, "https://gateway.test/d");
        own.Headers.Authorization = new AuthenticationHeaderValue("Basic", "x");
        await client.SendAsync(own);
        token = null;
        await client.GetAsync("https://gateway.test/e");
        token = "";
        await client.GetAsync("https://gateway.test/f");

        Assert.Equal("Bearer abc", fake.Requests[0].Headers.Authorization!.ToString());
        Assert.Equal("Bearer abc", fake.Requests[1].Headers.Authorization!.ToString());
        Assert.Null(fake.Requests[2].Headers.Authorization);
        Assert.Equal("Basic x", fake.Requests[3].Headers.Authorization!.ToString());
        Assert.Null(fake.Requests[4].Headers.Authorization);
        Assert.Null(fake.Requests[5].Headers.Authorization);
    }

    [Fact]
    public void TheRelay_NeedsATokenSource_AndAtLeastOneHost()
    {
        Assert.Throws<ArgumentNullException>(() => new BearerTokenRelayHandler(null!, ["a"]));
        Assert.Throws<ArgumentNullException>(() => new BearerTokenRelayHandler(() => null, null!));
        Assert.Throws<ArgumentException>(() => new BearerTokenRelayHandler(() => null, []));
        Assert.Throws<ArgumentException>(() => new BearerTokenRelayHandler(() => null, [" ", ""]));
    }

    // ---------------------------------------------------------------- the options and the resilience

    [Fact]
    public void TheOptions_AreValidated()
    {
        static void Invalid(string key, string value)
        {
            ServiceCollection services = new();
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => services.AddHttpClient("c").AddBaseHttpClientDefaults(Config(($"G:{key}", value)), "G"));
            Assert.Contains($"G:{key}", error.Message, StringComparison.Ordinal);
        }

        Invalid("AttemptTimeoutSeconds", "0");
        Invalid("TotalTimeoutSeconds", "5");
        Invalid("MaxRetries", "11");
        Invalid("RetryDelayMilliseconds", "-1");
        Invalid("CorrelationIdHeader", " ");
        Invalid("BaseUrl", "not a url");
        Invalid("BaseUrl", "ftp://files.test");
        Assert.Throws<ArgumentNullException>(() => ((IHttpClientBuilder)null!).AddBaseHttpClientDefaults(Config(), "G"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddHttpClient("c").AddBaseHttpClientDefaults(Config(), " "));
    }

    private static (HttpClient Client, Fake Fake) Pipeline(Func<HttpRequestMessage, HttpResponseMessage> answer, params (string Key, string? Value)[] settings)
    {
        Fake fake = new(answer);
        ServiceCollection services = new();
        services.AddHttpClient("c").AddBaseHttpClientDefaults(Config([.. settings, ("G:BaseUrl", "https://gateway.test"), ("G:RetryDelayMilliseconds", "1")]), "G").ConfigurePrimaryHttpMessageHandler(() => fake);
        ServiceProvider provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IHttpClientFactory>().CreateClient("c"), fake);
    }

    [Fact]
    public async Task TheClient_HasItsAddress_AndTheCorrelationIdOfTheChain()
    {
        (HttpClient client, Fake fake) = Pipeline(_ => new HttpResponseMessage(HttpStatusCode.OK));

        using (LogFlow.Begin("chain-2", "scope", "step"))
        {
            await client.GetAsync("/a");
        }

        Assert.Equal(new Uri("https://gateway.test"), client.BaseAddress);
        Assert.Equal("chain-2", fake.Requests[0].Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task AReadIsTriedAgain_AWriteIsNot_UnlessTheClientSaysItsWritesAreSafe()
    {
        (HttpClient client, Fake fake) = Pipeline(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), ("G:MaxRetries", "2"));
        (HttpClient safe, Fake safeFake) = Pipeline(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), ("G:MaxRetries", "2"), ("G:RetryUnsafeMethods", "true"));

        await client.GetAsync("/read");
        int reads = fake.Requests.Count;
        fake.Requests.Clear();
        await client.PostAsync("/write", new StringContent("{}"));
        int writes = fake.Requests.Count;
        await safe.PostAsync("/search", new StringContent("{}"));

        Assert.Equal(3, reads);
        Assert.Equal(1, writes);
        Assert.Equal(3, safeFake.Requests.Count);
    }

    [Fact]
    public async Task ARetryThatSucceeds_ReturnsTheAnswer()
    {
        int calls = 0;
        (HttpClient client, _) = Pipeline(_ => ++calls < 3 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : new HttpResponseMessage(HttpStatusCode.OK), ("G:MaxRetries", "3"));

        HttpResponseMessage response = await client.GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task TheCircuitBreakerCanBeSwitchedOff_AndNoRetriesIsAnOption()
    {
        (HttpClient client, Fake fake) = Pipeline(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError), ("G:MaxRetries", "0"), ("G:CircuitBreakerEnabled", "false"));

        for (int call = 0; call < 25; call++)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.GetAsync("/x")).StatusCode);
        }

        Assert.Equal(25, fake.Requests.Count);
    }

    [Fact]
    public async Task TheBearerRelay_IsAPartOfTheClient_AndRedirectsAreOff()
    {
        Fake fake = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        ServiceCollection services = new();
        services.AddHttpClient("r").AddBearerTokenRelay(_ => "tok", "gateway.test").ConfigurePrimaryHttpMessageHandler(() => fake);
        ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("r");

        await client.GetAsync("https://gateway.test/a");
        await client.GetAsync("https://other.test/a");

        Assert.Equal("Bearer tok", fake.Requests[0].Headers.Authorization!.ToString());
        Assert.Null(fake.Requests[1].Headers.Authorization);
        Assert.Throws<ArgumentNullException>(() => ((IHttpClientBuilder)null!).AddBearerTokenRelay(_ => null, "a"));
        Assert.Throws<ArgumentNullException>(() => services.AddHttpClient("x").AddBearerTokenRelay(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => services.AddHttpClient("y").AddBearerTokenRelay(_ => null, null!));
    }
}
