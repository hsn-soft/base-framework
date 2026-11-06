using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.AspNetCore.Tests;

public class SecurityHeadersAndCorsTests
{
    private static async Task<WebApplication> StartAsync(Dictionary<string, string?>? settings = null, string environment = "Production", bool https = false, Action<WebApplication>? before = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        builder.AddBaseHosting();
        WebApplication app = builder.Build();
        if (https)
        {
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next(context);
            });
        }

        app.UseBaseSecurityHeaders();
        app.UseBaseCors();
        before?.Invoke(app);
        app.MapGet("/", () => "ok");
        app.MapGet("/framed", (HttpContext context) =>
        {
            context.Response.Headers.XFrameOptions = "SAMEORIGIN";
            return "framed";
        });
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> GetAsync(WebApplication app, string path = "/", string? origin = null, HttpMethod? method = null, string? requestMethod = null)
    {
        using HttpRequestMessage request = new(method ?? HttpMethod.Get, path);
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin);
        }

        if (requestMethod is not null)
        {
            request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", requestMethod);
        }

        return await app.GetTestClient().SendAsync(request);
    }

    private static string? Header(HttpResponseMessage response, string name) => response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : null;

    // ---------------------------------------------------------------- security headers

    [Fact]
    public async Task TheSecurityHeaders_AreOnByDefault_AndHstsOnlyOnHttpsOutsideDevelopment()
    {
        await using WebApplication http = await StartAsync();
        await using WebApplication https = await StartAsync(https: true);
        await using WebApplication development = await StartAsync(environment: "Development", https: true);

        HttpResponseMessage plain = await GetAsync(http);
        HttpResponseMessage secure = await GetAsync(https);
        HttpResponseMessage local = await GetAsync(development);

        Assert.Equal("nosniff", Header(plain, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(plain, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(plain, "Referrer-Policy"));
        Assert.Null(Header(plain, "Strict-Transport-Security"));
        Assert.Null(Header(plain, "Content-Security-Policy"));
        Assert.Null(Header(plain, "Permissions-Policy"));
        Assert.Equal("max-age=31536000", Header(secure, "Strict-Transport-Security"));
        Assert.Null(Header(local, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task TheSecurityHeaders_FollowTheSettings()
    {
        await using WebApplication app = await StartAsync(new()
        {
            ["Hosting:SecurityHeaders:FrameOptions"] = "SAMEORIGIN",
            ["Hosting:SecurityHeaders:ReferrerPolicy"] = "",
            ["Hosting:SecurityHeaders:HstsMaxAgeSeconds"] = "600",
            ["Hosting:SecurityHeaders:HstsIncludeSubDomains"] = "true",
            ["Hosting:SecurityHeaders:ContentSecurityPolicy"] = "default-src 'self'",
            ["Hosting:SecurityHeaders:PermissionsPolicy"] = "camera=()",
        }, https: true);

        HttpResponseMessage response = await GetAsync(app);

        Assert.Equal("SAMEORIGIN", Header(response, "X-Frame-Options"));
        Assert.Null(Header(response, "Referrer-Policy"));
        Assert.Equal("max-age=600; includeSubDomains", Header(response, "Strict-Transport-Security"));
        Assert.Equal("default-src 'self'", Header(response, "Content-Security-Policy"));
        Assert.Equal("camera=()", Header(response, "Permissions-Policy"));
    }

    [Fact]
    public async Task AHeaderTheAnswerAlreadyHas_IsLeftAsItIs_AndNoHstsWhenItsAgeIsZero()
    {
        await using WebApplication app = await StartAsync(new() { ["Hosting:SecurityHeaders:HstsMaxAgeSeconds"] = "0" }, https: true);

        HttpResponseMessage framed = await GetAsync(app, "/framed");
        HttpResponseMessage plain = await GetAsync(app);

        Assert.Equal("SAMEORIGIN", Header(framed, "X-Frame-Options"));
        Assert.Null(Header(plain, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task TheSecurityHeaders_CanBeSwitchedOff()
    {
        await using WebApplication app = await StartAsync(new() { ["Hosting:SecurityHeaders:Enabled"] = "false" });

        HttpResponseMessage response = await GetAsync(app);

        Assert.Null(Header(response, "X-Content-Type-Options"));
        Assert.Null(Header(response, "X-Frame-Options"));
    }

    // ---------------------------------------------------------------- CORS

    [Fact]
    public async Task WithNoOriginListed_NoOtherOriginIsAllowed()
    {
        await using WebApplication app = await StartAsync();

        HttpResponseMessage response = await GetAsync(app, origin: "https://backoffice.example.com");

        Assert.Null(Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task AListedOrigin_MayCall_AndTheOthersMayNot()
    {
        await using WebApplication app = await StartAsync(new()
        {
            ["Hosting:Cors:AllowedOrigins:0"] = "https://backoffice.example.com/",
            ["Hosting:Cors:AllowedOrigins:1"] = "http://localhost:7300",
        });

        HttpResponseMessage listed = await GetAsync(app, origin: "https://backoffice.example.com");
        HttpResponseMessage local = await GetAsync(app, origin: "http://localhost:7300");
        HttpResponseMessage other = await GetAsync(app, origin: "https://evil.example.com");
        HttpResponseMessage preflight = await GetAsync(app, origin: "https://backoffice.example.com", method: HttpMethod.Options, requestMethod: "PUT");

        Assert.Equal("https://backoffice.example.com", Header(listed, "Access-Control-Allow-Origin"));
        Assert.Equal("X-Correlation-Id,Content-Disposition", Header(listed, "Access-Control-Expose-Headers"));
        Assert.Equal("http://localhost:7300", Header(local, "Access-Control-Allow-Origin"));
        Assert.Null(Header(other, "Access-Control-Allow-Origin"));
        Assert.Null(Header(listed, "Access-Control-Allow-Credentials"));
        Assert.Equal("GET,POST,PUT,PATCH,DELETE", Header(preflight, "Access-Control-Allow-Methods"));
        Assert.Equal("600", Header(preflight, "Access-Control-Max-Age"));
    }

    [Fact]
    public async Task Credentials_AreAllowedOnlyWhenTheSettingsSayAndTheOriginsAreNamed()
    {
        await using WebApplication app = await StartAsync(new()
        {
            ["Hosting:Cors:AllowedOrigins:0"] = "https://backoffice.example.com",
            ["Hosting:Cors:AllowCredentials"] = "true",
            ["Hosting:Cors:PreflightMaxAgeSeconds"] = "30",
            ["Hosting:Cors:AllowedMethods:0"] = "GET",
        });

        HttpResponseMessage response = await GetAsync(app, origin: "https://backoffice.example.com");
        HttpResponseMessage preflight = await GetAsync(app, origin: "https://backoffice.example.com", method: HttpMethod.Options, requestMethod: "GET");

        Assert.Equal("true", Header(response, "Access-Control-Allow-Credentials"));
        Assert.Equal("30", Header(preflight, "Access-Control-Max-Age"));
    }

    [Fact]
    public async Task AWildcard_AllowsAnyOrigin_WithoutCredentials()
    {
        await using WebApplication app = await StartAsync(new() { ["Hosting:Cors:AllowedOrigins:0"] = "*" });

        HttpResponseMessage response = await GetAsync(app, origin: "https://anyone.example.com");

        Assert.Equal("*", Header(response, "Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("*", true, "cannot be used with AllowCredentials")]
    [InlineData("backoffice.example.com", false, "is not an origin")]
    [InlineData("https://backoffice.example.com/admin", false, "is not an origin")]
    [InlineData("ftp://backoffice.example.com", false, "is not an origin")]
    [InlineData("https://backoffice.example.com?x=1", false, "is not an origin")]
    public void ASettingThatCannotBeRight_StopsTheStart(string origin, bool credentials, string reason)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Hosting:Cors:AllowedOrigins:0"] = origin, ["Hosting:Cors:AllowCredentials"] = credentials.ToString() });

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => builder.AddBaseHosting());

        Assert.Contains(reason, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePreflightAge_IsBounded()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Hosting:Cors:AllowedOrigins:0"] = "https://a.example.com", ["Hosting:Cors:PreflightMaxAgeSeconds"] = "100000" });

        Assert.Throws<InvalidOperationException>(() => builder.AddBaseHosting());
    }

    [Fact]
    public async Task TheArgumentsAreChecked()
    {
        await using WebApplication app = await StartAsync();

        Assert.Throws<ArgumentNullException>(() => ((WebApplication)null!).UseBaseSecurityHeaders());
        Assert.Throws<ArgumentNullException>(() => ((WebApplication)null!).UseBaseCors());
    }
}
