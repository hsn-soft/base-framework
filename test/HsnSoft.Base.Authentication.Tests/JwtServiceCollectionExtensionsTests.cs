using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HsnSoft.Base.Authentication.Tests;

public class JwtServiceCollectionExtensionsTests
{
    private const string Key = "unit-test-signing-key-0123456789-abcdef";

    private static ServiceProvider BuildProvider(Dictionary<string, string?>? settings = null)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = Key,
                ["Jwt:AccessTokenMinutes"] = "5",
            })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddRouting()   // the authorization middleware needs the routing services
            .AddBaseJwtAuthentication(configuration)
            .BuildServiceProvider();
    }

    private static TokenValidationParameters Parameters(ServiceProvider provider)
        => provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

    private static string CreateToken(string key, string issuer = "test-issuer", string audience = "test-audience", string role = "admin")
        => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("sub", "alice"), new System.Security.Claims.Claim("role", role)]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });

    [Fact]
    public void JwtSection_IsBoundToTheOptions()
    {
        using ServiceProvider provider = BuildProvider();

        JwtOptions options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal("test-issuer", options.Issuer);
        Assert.Equal("test-audience", options.Audience);
        Assert.Equal(Key, options.Key);
        Assert.Equal(5, options.AccessTokenMinutes);
    }

    [Fact]
    public void JwtOptions_HaveSafeDefaults()
    {
        var options = new JwtOptions();

        Assert.Equal(string.Empty, options.Issuer);
        Assert.Equal(string.Empty, options.Audience);
        Assert.Equal(string.Empty, options.Key);
        Assert.Equal(15, options.AccessTokenMinutes);
    }

    [Fact]
    public void BearerOptions_UseTheConfiguredIssuerAudienceAndShortClaimNames()
    {
        using ServiceProvider provider = BuildProvider();

        JwtBearerOptions bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        TokenValidationParameters parameters = bearer.TokenValidationParameters;

        Assert.Equal("test-issuer", parameters.ValidIssuer);
        Assert.Equal("test-audience", parameters.ValidAudience);
        Assert.Equal("sub", parameters.NameClaimType);
        Assert.Equal("role", parameters.RoleClaimType);
        Assert.Equal(TimeSpan.FromSeconds(30), parameters.ClockSkew);
        Assert.False(bearer.MapInboundClaims);
    }

    [Fact]
    public async Task ATokenSignedWithTheConfiguredKey_IsValid()
    {
        using ServiceProvider provider = BuildProvider();

        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(CreateToken(Key), Parameters(provider));

        Assert.True(result.IsValid);
        Assert.Equal("alice", result.ClaimsIdentity.Name);
        Assert.True(new System.Security.Claims.ClaimsPrincipal(result.ClaimsIdentity).IsInRole("admin"));
    }

    [Theory]
    [InlineData("another-signing-key-0123456789-abcdef-xyz", "test-issuer", "test-audience")]
    [InlineData(Key, "someone-else", "test-audience")]
    [InlineData(Key, "test-issuer", "another-audience")]
    public async Task ATokenWithAWrongKeyIssuerOrAudience_IsRejected(string key, string issuer, string audience)
    {
        using ServiceProvider provider = BuildProvider();

        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(CreateToken(key, issuer, audience), Parameters(provider));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task TheAuthenticatedPolicy_RequiresAnAuthenticatedUser()
    {
        using ServiceProvider provider = BuildProvider();

        AuthorizationPolicy? policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(BasePolicies.Authenticated);

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
        Assert.Equal("authenticated", BasePolicies.Authenticated);
    }

    [Fact]
    public async Task ThePipelineMiddleware_RunsWithoutAToken()
    {
        using ServiceProvider provider = BuildProvider();
        var app = new ApplicationBuilder(provider);
        bool reached = false;

        Assert.Same(app, app.UseBaseAuthentication());
        Assert.Same(app, app.UseBaseAuthorization());
        app.Run(_ => { reached = true; return Task.CompletedTask; });

        var context = new DefaultHttpContext { RequestServices = provider };
        await app.Build()(context);

        Assert.True(reached);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => JwtServiceCollectionExtensions.AddBaseJwtAuthentication(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseJwtAuthentication(null!));
        Assert.Throws<ArgumentNullException>(() => JwtServiceCollectionExtensions.UseBaseAuthentication(null!));
        Assert.Throws<ArgumentNullException>(() => JwtServiceCollectionExtensions.UseBaseAuthorization(null!));
    }
}
