using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

// a DTO that uses every rule, to show what a client is told for each rule
public enum SampleKind
{
    Standard = 1,
    Premium = 2,
}

public sealed class SampleAddress
{
    [Required]
    [StringLength(100)]
    public string? City { get; set; }
}

/// <summary>
/// A model of an update DTO that uses every rule of the framework (ADR-0044). It is not used by the service: copy the parts you need.
/// Every message the client gets is translated (en, tr, ru) and listed in <c>statusMessages</c>, one for each wrong field.
/// </summary>
public sealed class SampleUpdate : IValidatableObject
{
    /// <summary>The id: nullable so a missing one is told (<c>[Required]</c>), and <c>[NotDefault]</c> refuses <c>Guid.Empty</c>.</summary>
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>A text that must be sent: <c>[Required]</c> also refuses <c>""</c> and white space; the limit is told with its number.</summary>
    [Required]
    [StringLength(64)]
    public string? Name { get; set; }

    /// <summary>An optional text: when it is given it must not be blank (<c>[NotWhiteSpace]</c>); not given is fine.</summary>
    [NotWhiteSpace]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [RegularExpression("^[A-Z0-9-]{3,20}$")]
    public string? Sku { get; set; }

    /// <summary>A number that must be sent: nullable with <c>[Required]</c>, so a missing one is not <c>0</c>; <c>"abc"</c> or <c>1.5</c> for an int is an invalid value without any attribute.</summary>
    [Required]
    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }

    [Required]
    [Range(1, 10_000)]
    public int? Quantity { get; set; }

    /// <summary>A bool that must be sent: nullable with <c>[Required]</c>, so a missing one is not <c>false</c>.</summary>
    [Required]
    public bool? IsActive { get; set; }

    /// <summary>An enum: nullable with <c>[Required]</c>; <c>[EnumDataType]</c> refuses a number that is not defined (a name that is not defined is refused by the JSON converter).</summary>
    [Required]
    [EnumDataType(typeof(SampleKind))]
    public SampleKind? Kind { get; set; }

    /// <summary>A text that must repeat another (<c>[Compare]</c>).</summary>
    [Compare(nameof(Email))]
    public string? EmailAgain { get; set; }

    /// <summary>An object: <c>[Required]</c> is needed to refuse <c>null</c> (a non-nullable type is not required by itself); the fields inside are checked too, as <c>Address.City</c>.</summary>
    [Required]
    public SampleAddress? Address { get; set; }

    public SampleAddress? BillingAddress { get; set; }

    /// <summary>A list that must have at least one item (<c>[MinLength]</c>) and no blank item (<c>[NoBlankItems]</c>).</summary>
    [MinLength(1)]
    [NoBlankItems]
    public List<string>? Tags { get; set; }

    /// <summary>A rule that depends on other fields: it runs only when every field above is valid. The text is a key of the localization files, with its arguments.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Kind == SampleKind.Premium && Price < 100)
        {
            yield return ValidationRule.Failed(nameof(Price), "Validation:Range", nameof(Price), 100, 1_000_000);
        }
    }
}

[ApiController]
[Route("api/sample")]
public sealed class SampleController : ControllerBase
{
    [HttpPut]
    public IActionResult Put([FromBody] SampleUpdate input) => Ok();
}

public sealed class SampleUpdateTests
{
    private static async Task<(HttpStatusCode Status, string[] Messages)> PutAsync(string json, string language = "en")
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi();
        builder.Services.AddBaseLocalization();
        builder.Services.AddControllers().AddApplicationPart(typeof(SampleController).Assembly);
        await using WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.UseBaseLocalization();
        app.MapControllers();
        await app.StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Put, "/api/sample") { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.TryAddWithoutValidation("Accept-Language", language);
        HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        JsonElement body = JsonDocument.Parse(text).RootElement;
        return (response.StatusCode, body.TryGetProperty("statusMessages", out JsonElement messages) ? [.. messages.EnumerateArray().Select(m => m.GetString()!)] : []);
    }

    private const string Valid = """{"id":"6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11","name":"Lamp","email":"a@b.co","emailAgain":"a@b.co","price":150,"quantity":2,"isActive":true,"kind":"Premium","address":{"city":"Ankara"},"tags":["a"]}""";

    [Fact]
    public async Task AValidRequest_Passes()
    {
        (HttpStatusCode status, string[] messages) = await PutAsync(Valid);

        Assert.True(status == HttpStatusCode.OK, string.Join(" | ", messages));
    }

    [Fact]
    public async Task EveryRuleAnswers_InTheLanguageOfTheRequest()
    {
        const string bad = """{"id":"00000000-0000-0000-0000-000000000000","name":"  ","description":" ","email":"nope","phone":"x","sku":"a b","price":-5,"quantity":0,"kind":99,"emailAgain":"other","address":{"city":""},"billingAddress":{"city":" "},"tags":["a",""]}""";

        (HttpStatusCode status, string[] messages) = await PutAsync(bad, "tr");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(14, messages.Length);
        Assert.Equal("Doğrulama başarısız.", messages[0]);
        Assert.Contains("Name zorunludur.", messages);
        Assert.Contains("Id geçerli bir değer değil.", messages);
        Assert.Contains("Kind geçerli bir değer değil.", messages);
        Assert.Contains("Tags boş eleman içeremez.", messages);
        Assert.Contains("Quantity 1 ile 10000 arasında olmalı.", messages);
        Assert.Contains("City zorunludur.", messages);
    }

    [Fact]
    public async Task ARuleThatDependsOnOtherFields_RunsWhenTheFieldsAreValid()
    {
        (HttpStatusCode status, string[] messages) = await PutAsync(Valid.Replace("\"price\":150", "\"price\":50"));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.True(messages.SequenceEqual(["Validation failed.", "Price must be between 100 and 1000000."]), string.Join(" | ", messages));
    }
}
