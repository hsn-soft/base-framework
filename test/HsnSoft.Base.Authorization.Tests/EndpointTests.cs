using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Authorization.Tests;

/// <summary>The attribute on a real endpoint: signed in with a role that was granted, signed in with one that was not, and not signed in.</summary>
public class EndpointTests
{
    private sealed class HeaderAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Role", out Microsoft.Extensions.Primitives.StringValues role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ClaimsIdentity identity = new([new Claim("role", role.ToString())], "header", nameType: "name", roleType: "role");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "header")));
        }
    }

    private sealed class RoleUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

        public Guid? Id => null;

        public string? UserName => null;

        public IReadOnlyList<string> Roles => [.. accessor.HttpContext?.User.FindAll("role").Select(claim => claim.Value) ?? []];
    }

    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("header").AddScheme<AuthenticationSchemeOptions, HeaderAuthentication>("header", null);
        builder.Services.AddDistributedMemoryCache().AddHttpContextAccessor().AddScoped<ICurrentUser, RoleUser>();
        builder.Services.AddBasePermissions(new ConfigurationBuilder().Build());

        WebApplication app = builder.Build();
        await app.Services.GetRequiredService<IPermissionStore>().SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), new PermissionGrants(new Dictionary<string, string?> { [TestCodes.Files.Read] = null }));
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/read", [PermissionAuthorize(TestCodes.Files.Read)] () => "read");
        app.MapGet("/write", [PermissionAuthorize(TestCodes.Files.Write)] () => "write");
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    private static async Task<HttpStatusCode> CallAsync(HttpClient client, string path, string? role)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        if (role is not null)
        {
            request.Headers.Add("X-Role", role);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task AnEndpoint_LetsInTheCallerThatWasGranted_RefusesOneThatWasNot_AndAsksTheOneWhoIsNotSignedInToSignIn()
    {
        (WebApplication app, HttpClient client) = await StartAsync();
        await using (app)
        {
            Assert.Equal(HttpStatusCode.OK, await CallAsync(client, "/read", "operator"));
            Assert.Equal(HttpStatusCode.Forbidden, await CallAsync(client, "/write", "operator"));
            Assert.Equal(HttpStatusCode.Forbidden, await CallAsync(client, "/read", "stranger"));
            Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync(client, "/read", null));
        }
    }
}
