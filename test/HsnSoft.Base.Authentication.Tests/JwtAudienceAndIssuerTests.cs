using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HsnSoft.Base.Authentication.Tests;

public class JwtAudienceAndIssuerTests
{
    private const string Key = "unit-test-signing-key-0123456789-abcdef";

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = "";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ServiceProvider Provider(string? environment, params (string Key, string Value)[] more)
    {
        Dictionary<string, string?> settings = new() { ["Jwt:Issuer"] = "the-issuer", ["Jwt:Audience"] = "abc-order", ["Jwt:Key"] = Key };
        foreach ((string key, string value) in more)
        {
            settings[key] = value;
        }

        ServiceCollection services = new();
        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new Environment(environment));
        }

        return services.AddLogging().AddBaseJwtAuthentication(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()).BuildServiceProvider();
    }

    private static Task<TokenValidationResult> Validate(ServiceProvider provider, string issuer, params string[] audiences)
    {
        TokenValidationParameters parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer,
            Subject = new ClaimsIdentity([new Claim("sub", "alice")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object> { ["aud"] = audiences },
        };
        return new JsonWebTokenHandler().ValidateTokenAsync(new JsonWebTokenHandler().CreateToken(descriptor), parameters);
    }

    [Fact]
    public async Task ATokenThatNamesTheAudienceOfTheHost_IsAccepted_AlsoAmongOthers()
    {
        using ServiceProvider provider = Provider("production");

        Assert.True((await Validate(provider, "the-issuer", "abc-order")).IsValid);
        Assert.True((await Validate(provider, "the-issuer", "abc-product", "abc-order", "abc-shipment")).IsValid);
    }

    [Fact]
    public async Task ATokenThatDoesNotNameTheAudienceOfTheHost_IsRejected()
    {
        using ServiceProvider provider = Provider("production");

        Assert.False((await Validate(provider, "the-issuer", "abc-product", "abc-shipment")).IsValid);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("uat")]
    [InlineData("docker")]
    [InlineData("staging")]
    public async Task TheIssuer_IsCheckedInEveryEnvironmentButDevelopment(string environment)
    {
        using ServiceProvider provider = Provider(environment);

        Assert.False((await Validate(provider, "another-issuer", "abc-order")).IsValid);
        Assert.True((await Validate(provider, "the-issuer", "abc-order")).IsValid);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    public async Task InDevelopment_TheIssuerIsNotChecked(string environment)
    {
        using ServiceProvider provider = Provider(environment);

        Assert.True((await Validate(provider, "another-issuer", "abc-order")).IsValid);
    }

    [Fact]
    public async Task WithoutAnEnvironment_TheIssuerIsChecked()
    {
        using ServiceProvider provider = Provider(null);

        Assert.False((await Validate(provider, "another-issuer", "abc-order")).IsValid);
    }

    [Fact]
    public async Task TheSetting_SaysItForAnEnvironment()
    {
        using ServiceProvider production = Provider("production", ("Jwt:ValidateIssuer", "false"));
        using ServiceProvider development = Provider("development", ("Jwt:ValidateIssuer", "true"));

        Assert.True((await Validate(production, "another-issuer", "abc-order")).IsValid);
        Assert.False((await Validate(development, "another-issuer", "abc-order")).IsValid);
    }

    [Fact]
    public void TheClockSkew_IsThirtySecondsOrTheSetting()
    {
        using ServiceProvider byDefault = Provider("production");
        using ServiceProvider set = Provider("production", ("Jwt:ClockSkewSeconds", "5"));

        static TimeSpan Skew(ServiceProvider provider) => provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.ClockSkew;

        Assert.Equal(TimeSpan.FromSeconds(30), Skew(byDefault));
        Assert.Equal(TimeSpan.FromSeconds(5), Skew(set));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("301")]
    public void AClockSkewOutOfRange_StopsTheHost(string seconds)
    {
        Assert.Contains("Jwt:ClockSkewSeconds", Assert.Throws<InvalidOperationException>(() => Provider("production", ("Jwt:ClockSkewSeconds", seconds))).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStandardAuthorization_IsItsOwnCall()
    {
        using ServiceProvider without = Provider("production");
        ServiceCollection services = new();
        services.AddBaseAuthorization();
        using ServiceProvider with = services.BuildServiceProvider();

        Assert.Null(without.GetService<IAuthorizationPolicyProvider>());
        Assert.NotNull(with.GetRequiredService<IAuthorizationPolicyProvider>());
        Assert.Throws<ArgumentNullException>(() => JwtServiceCollectionExtensions.AddBaseAuthorization(null!));
    }
}
