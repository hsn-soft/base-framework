using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

public sealed class Form
{
    [Required]
    public string? Name { get; set; }
}

[ApiController]
[Route("api/localized")]
public sealed class LocalizedController : ControllerBase
{
    [HttpGet("missing")]
    public IActionResult Missing() => throw new NotFoundException("order 7 is missing").WithKey("order_not_found", 7);

    [HttpGet("rule")]
    public IActionResult Rule() => throw new BusinessException("stock_empty", "stock is empty").WithData("Product", "lamp");

    [HttpGet("rule-with-args")]
    public IActionResult RuleWithArgs() => throw new BusinessException("sku_taken", "the sku is taken").WithKey("sku_taken", "LAMP-1");

    [HttpGet("domain")]
    public IActionResult Domain() => throw new DomainException("name is required").WithKey("field_required", "Name", 64);

    [HttpGet("business-rule")]
    public IActionResult BusinessRule() => throw new BusinessRuleException("not allowed").WithKey("not_allowed");

    [HttpGet("http")]
    public IActionResult Http() => throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

    [HttpGet("no-text")]
    public IActionResult NoText() => throw new NotFoundException("the plain message").WithKey("no_such_key");

    [HttpGet("boom")]
    public IActionResult Boom() => throw new InvalidOperationException("secret");

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] Form form) => Ok(form);

    [HttpGet("ok")]
    public IActionResult Fine() => Ok(new { fine = true });
}

public sealed class LocalizedAnswersTests
{
    private static string Json(string language, params (string Key, string Text)[] texts)
        => JsonSerializer.Serialize(new { culture = language, texts = texts.ToDictionary(pair => pair.Key, pair => pair.Text) });

    private static readonly Dictionary<string, string> s_service = new()
    {
        ["en"] = Json("en", ("order_not_found", "Order {0} was not found."), ("stock_empty", "The stock is empty."), ("sku_taken", "The sku '{0}' is taken."), ("field_required", "{0} is required and at most {1} characters."), ("not_allowed", "Not allowed."), ("invalid_credentials", "Invalid credentials.")),
        ["tr"] = Json("tr", ("order_not_found", "{0} numaralı sipariş bulunamadı."), ("stock_empty", "Stok tükendi."), ("sku_taken", "'{0}' kodu kullanılıyor."), ("field_required", "{0} zorunludur ve en fazla {1} karakter olabilir."), ("not_allowed", "İzin yok."), ("invalid_credentials", "Geçersiz kimlik bilgileri.")),
        ["ru"] = Json("ru", ("order_not_found", "Заказ {0} не найден."), ("stock_empty", "Товар закончился."), ("sku_taken", "Код '{0}' занят."), ("field_required", "{0} обязательно, не более {1} символов."), ("not_allowed", "Нельзя."), ("invalid_credentials", "Неверные учётные данные.")),
    };

    private static async Task<WebApplication> StartAsync(bool localized = true, bool useLanguage = true)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi();
        if (localized)
        {
            builder.Services.AddBaseLocalization(configure: options => options.AddJson("service", language => s_service.GetValueOrDefault(language)));
        }

        builder.Services.AddControllers().AddApplicationPart(typeof(LocalizedController).Assembly);
        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();

        // what the authentication does: the principal of the token is on the request
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Test-Locale", out Microsoft.Extensions.Primitives.StringValues locale))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("locale", locale.ToString())], "Bearer"));
            }

            await next();
        });
        if (useLanguage)
        {
            app.UseBaseLocalization();
        }

        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> GetAsync(WebApplication app, string path, string? acceptLanguage = null, string? tokenLocale = null, HttpMethod? method = null, string? content = null)
    {
        using HttpRequestMessage request = new(method ?? HttpMethod.Get, path);
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        if (tokenLocale is not null)
        {
            request.Headers.Add("X-Test-Locale", tokenLocale);
        }

        if (content is not null)
        {
            request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        return (response, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private static string Message(JsonElement body, int index = 0) => body.GetProperty("statusMessages")[index].GetString()!;

    [Theory]
    [InlineData("tr", "7 numaralı sipariş bulunamadı.")]
    [InlineData("ru", "Заказ 7 не найден.")]
    [InlineData("en", "Order 7 was not found.")]
    public async Task AnErrorThatNamesATextIsToldInTheLanguageOfTheHeader(string language, string expected)
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/localized/missing", acceptLanguage: language);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(expected, Message(body));
        Assert.Equal(language, response.Content.Headers.ContentLanguage.Single());
    }

    [Fact]
    public async Task TheLanguageOfTheToken_IsBeforeTheHeader_AndTheHeaderIsBeforeTheDefault()
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement withToken) = await GetAsync(app, "/api/localized/missing", acceptLanguage: "ru", tokenLocale: "tr-TR");
        (_, JsonElement withHeader) = await GetAsync(app, "/api/localized/missing", acceptLanguage: "ru");
        (_, JsonElement withNothing) = await GetAsync(app, "/api/localized/missing");
        (_, JsonElement unsupportedToken) = await GetAsync(app, "/api/localized/missing", acceptLanguage: "tr", tokenLocale: "de");

        Assert.Equal("7 numaralı sipariş bulunamadı.", Message(withToken));
        Assert.Equal("Заказ 7 не найден.", Message(withHeader));
        Assert.Equal("Order 7 was not found.", Message(withNothing));
        Assert.Equal("7 numaralı sipariş bulunamadı.", Message(unsupportedToken));
    }

    [Theory]
    [InlineData("de-DE,ru;q=0.8,tr;q=0.5", "Заказ 7 не найден.")]
    [InlineData("tr;q=0.4, ru;q=0.9", "Заказ 7 не найден.")]
    [InlineData("tr;q=0, ru;q=0.1", "Заказ 7 не найден.")]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", "7 numaralı sipariş bulunamadı.")]
    [InlineData("de, fr;q=0.5, *;q=0.1", "Order 7 was not found.")]
    [InlineData("not a language header ;;;", "Order 7 was not found.")]
    public async Task TheHeader_IsReadWithItsQualities_AndWhatIsNotSupportedIsSkipped(string header, string expected)
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, "/api/localized/missing", acceptLanguage: header);

        Assert.Equal(expected, Message(body));
    }

    [Fact]
    public async Task TheErrorCode_IsTheKey_AndTheCodeLineIsToldToo()
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/localized/rule", acceptLanguage: "tr");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Stok tükendi.", Message(body));
        Assert.Equal("Hata kodu: stock_empty", Message(body, 1));
        Assert.Equal("Product: lamp", Message(body, 2));
        Assert.Equal("stock_empty", body.GetProperty("errorCode").GetString());
    }

    [Theory]
    [InlineData("/api/localized/rule-with-args", "tr", "'LAMP-1' kodu kullanılıyor.")]
    [InlineData("/api/localized/domain", "ru", "Name обязательно, не более 64 символов.")]
    [InlineData("/api/localized/domain", "tr", "Name zorunludur ve en fazla 64 karakter olabilir.")]
    [InlineData("/api/localized/business-rule", "tr", "İzin yok.")]
    [InlineData("/api/localized/http", "tr", "Geçersiz kimlik bilgileri.")]
    public async Task EveryKindOfKnownError_CanNameATextWithItsArguments(string path, string language, string expected)
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, path, acceptLanguage: language);

        Assert.Equal(expected, Message(body));
    }

    [Fact]
    public async Task WithoutATextForTheKey_TheClientGetsTheMessage()
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, "/api/localized/no-text", acceptLanguage: "tr");

        Assert.Equal("the plain message", Message(body));
    }

    [Fact]
    public async Task AFailureOfTheService_IsNotTranslated_AndLeaksNothing()
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/localized/boom", acceptLanguage: "tr");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("INTERNAL_SERVER_ERROR", Message(body));
    }

    [Theory]
    [InlineData("tr", "Doğrulama başarısız.")]
    [InlineData("ru", "Ошибка проверки данных.")]
    [InlineData("en", "Validation failed.")]
    public async Task TheValidationMessageAndTheStatusMessages_AreTheFrameworksOwnTexts(string language, string expected)
    {
        await using WebApplication app = await StartAsync();

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/localized/validate", acceptLanguage: language, method: HttpMethod.Post, content: "{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expected, Message(body));
    }

    [Theory]
    [InlineData("/api/localized/ok", "tr", "Başarılı")]
    [InlineData("/api/nowhere", "tr", "Kaynak bulunamadı.")]
    [InlineData("/api/nowhere", "ru", "Ресурс не найден.")]
    [InlineData("/api/nowhere", "en", "Resource not found.")]
    public async Task TheStatusMessages_OfAnswersAndOfBodylessStatuses_AreToldInTheLanguageToo(string path, string language, string expected)
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement body) = await GetAsync(app, path, acceptLanguage: language);

        Assert.Equal(expected, Message(body));
    }

    [Fact]
    public async Task TheLanguageOfTheTokenReachesTheOuterHandlers_ThatTheMiddlewareDoesNot()
    {
        await using WebApplication app = await StartAsync();

        (_, JsonElement status) = await GetAsync(app, "/api/nowhere", tokenLocale: "ru");

        Assert.Equal("Ресурс не найден.", Message(status));
    }

    [Fact]
    public async Task WithoutTheLocalization_EverythingIsEnglishAsBefore()
    {
        await using WebApplication app = await StartAsync(localized: false);

        (HttpResponseMessage response, JsonElement body) = await GetAsync(app, "/api/localized/missing", acceptLanguage: "tr", tokenLocale: "tr");
        (_, JsonElement status) = await GetAsync(app, "/api/nowhere", acceptLanguage: "tr");
        (_, JsonElement validation) = await GetAsync(app, "/api/localized/validate", acceptLanguage: "tr", method: HttpMethod.Post, content: "{}");

        Assert.Equal("order 7 is missing", Message(body));
        Assert.Equal("Resource not found.", Message(status));
        Assert.Equal("Validation failed.", Message(validation));
        Assert.Empty(response.Content.Headers.ContentLanguage);
    }

    [Fact]
    public async Task TheMiddleware_IsLeftOutWhenTheLocalizationIsNot()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        Assert.Same(app, app.UseBaseLocalization());
        Assert.Throws<ArgumentNullException>(() => ((IApplicationBuilder)null!).UseBaseLocalization());
        await app.DisposeAsync();
    }

    [Fact]
    public async Task TheClaimOfTheLanguage_CanBeChosenInTheSettings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Localization:LanguageClaim"] = "user_lg" });
        builder.Services.AddBaseWebApi(builder.Configuration);
        builder.Services.AddBaseLocalization();
        await using WebApplication app = builder.Build();
        IRequestLanguageResolver resolver = app.Services.GetRequiredService<IRequestLanguageResolver>();

        DefaultHttpContext context = new() { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("user_lg", "ru"), new Claim("locale", "tr")], "Bearer")) };

        Assert.Equal("user_lg", app.Services.GetRequiredService<IOptions<RequestLanguageOptions>>().Value.LanguageClaim);
        Assert.Equal("ru", resolver.Resolve(context));
        Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null!));
    }

    [Fact]
    public async Task TheMiddleware_RejectsANullContext()
    {
        RequestLanguageMiddleware middleware = new(_ => Task.CompletedTask);

        await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!, null!, null!));
    }
}
