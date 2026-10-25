using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

public sealed class SignUp : IValidatableObject
{
    [Required]
    [StringLength(8)]
    public string? UserName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [NotDefault]
    public Guid? TenantId { get; set; }

    [Range(1, 10)]
    public int? Level { get; set; }

    [NotWhiteSpace]
    public string? Nickname { get; set; }

    [NoBlankItems]
    public List<string>? Roles { get; set; }

    public List<SignUpLine> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserName == "root")
        {
            yield return ValidationRule.Failed(nameof(UserName), "Validation:Required", nameof(UserName));
        }
    }
}

public sealed class SignUpLine
{
    [Required]
    public Guid? ProductId { get; set; }
}

[ApiController]
[Route("api/validation")]
public sealed class ValidationController : ControllerBase
{
    [HttpPost("sign-up")]
    public IActionResult SignUp([FromBody] SignUp input) => Ok(input.UserName);

    [HttpPost("business")]
    public IActionResult Business()
    {
        ValidationErrors errors = new();
        errors.Add("Name", "Name is required.", "Validation:Required", "Name");
        errors.AddIf(true, "Level", "Level is out of range.", "Validation:Range", "Level", 1, 10);
        errors.AddIf(false, "Never", "Never is required.", "Validation:Required", "Never");
        errors.ThrowIfAny();
        return Ok();
    }
}

public sealed class ValidationAnswersTests
{
    private static async Task<WebApplication> StartAsync(string environment)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi();
        builder.Services.AddBaseLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(ValidationController).Assembly);
        WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.UseBaseLocalization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(WebApplication app, string path, string json, string language = "en")
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.TryAddWithoutValidation("Accept-Language", language);
        HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, JsonDocument.Parse(text).RootElement.Clone());
    }

    private static string[] Messages(JsonElement body) => [.. body.GetProperty("statusMessages").EnumerateArray().Select(m => m.GetString()!)];

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task EveryFailingField_IsListedInTheMessages_AndInThePayload_InEveryEnvironment(string environment)
    {
        await using WebApplication app = await StartAsync(environment);

        (HttpStatusCode status, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"   ","email":"nope","level":99}""");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("validation_error", body.GetProperty("errorCode").GetString());
        string[] messages = Messages(body);
        Assert.Equal("Validation failed.", messages[0]);
        Assert.Contains("UserName is required.", messages);
        Assert.Contains(messages, m => m.Contains("Email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, m => m.Contains("TenantId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, m => m.Contains("Level", StringComparison.OrdinalIgnoreCase));
        Assert.False(body.TryGetProperty("payload", out _));
        Assert.Equal(5, messages.Length);
    }

    [Fact]
    public async Task AnInvalidGuid_IsTold_AsAnInvalidValue_OfThatField()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"ada","email":"a@b.co","tenantId":"not-a-guid"}""");

        Assert.Contains("TenantId has an invalid value.", Messages(body));
    }

    [Fact]
    public async Task ANestedField_IsNamedByItsPath()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"ada","email":"a@b.co","tenantId":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11","lines":[{}]}""");

        Assert.Contains("ProductId is required.", Messages(body));
    }

    [Fact]
    public async Task AMalformedBody_IsOneMessage()
    {
        await using WebApplication app = await StartAsync("Production");

        (HttpStatusCode status, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", "{ not json");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(2, Messages(body).Length);
    }

    [Fact]
    public async Task ATooLongText_IsTold_WithTheLimit()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"abcdefghijk","email":"a@b.co","tenantId":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11"}""");

        Assert.Contains(Messages(body), m => m.Contains('8'));
    }

    [Fact]
    public async Task ARuleOfTheObject_IsTranslated_LikeAnAttribute()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"root","email":"a@b.co","tenantId":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11"}""", "tr");

        Assert.Equal("UserName zorunludur.", Messages(body)[1]);
    }

    [Fact]
    public async Task ThePropertiesAreChecked_BeforeTheRulesOfTheObject()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"root"}""");

        // root alone would fail the rule of the object, but the missing fields stop the validation first
        Assert.Contains(Messages(body), m => m.StartsWith("Email", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("en", "Validation failed.")]
    [InlineData("tr", "Doğrulama başarısız.")]
    public async Task AValidationFailureThrownByTheCode_IsToldFieldByField_InTheLanguageOfTheRequest(string language, string header)
    {
        await using WebApplication app = await StartAsync("Production");

        (HttpStatusCode status, JsonElement body) = await PostAsync(app, "/api/validation/business", "{}", language);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(3, Messages(body).Length);
        Assert.Equal(header, Messages(body)[0]);
    }

    [Fact]
    public async Task TheEmptyGuid_IsRefused_ByNotDefault_AsAnInvalidValue()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement body) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"ada","email":"a@b.co","tenantId":"00000000-0000-0000-0000-000000000000"}""");

        Assert.Equal(["Validation failed.", "TenantId has an invalid value."], Messages(body));
    }

    [Fact]
    public async Task ABlankNickname_AndABlankRole_AreTold_ButAMissingOneIsNot()
    {
        await using WebApplication app = await StartAsync("Production");

        (_, JsonElement blank) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"ada","email":"a@b.co","tenantId":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11","nickname":"  ","roles":["admin",""]}""");
        (HttpStatusCode status, _) = await PostAsync(app, "/api/validation/sign-up", """{"userName":"ada","email":"a@b.co","tenantId":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11"}""");

        Assert.Equal(3, Messages(blank).Length);
        Assert.Contains("Nickname must not be empty.", Messages(blank));
        Assert.Contains("Roles must not have an empty item.", Messages(blank));
        Assert.Equal(HttpStatusCode.OK, status);
    }
}
