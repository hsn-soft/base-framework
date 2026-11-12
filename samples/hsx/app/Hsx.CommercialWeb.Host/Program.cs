using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Http;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;
using Hsx.CommercialWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Hsx.CommercialWeb");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseHealthChecks()
    .AddHttpContextAccessor();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
    options.Conventions.AllowAnonymousToPage("/Error");
}).AddMvcOptions(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/account/login";
    options.SlidingExpiration = false;   // the sign-in ends when its token does
});
builder.Services.AddAuthorization();

// every call to the commercial gateway carries the correlation id of the page being served and the token of the signed-in user (only to the host of the gateway); the page only reads, so a POST that failed is tried again
builder.Services
    .AddHttpClient<GatewayClient>()
    .AddBaseHttpClientDefaults(builder.Configuration, "Gateway", options => options.RetryUnsafeMethods = true)
    .AddBearerTokenRelay(
        services => services.GetRequiredService<IHttpContextAccessor>().HttpContext?.User.FindFirst("access_token")?.Value,
        new Uri(builder.Configuration["Gateway:BaseUrl"] ?? throw new InvalidOperationException("Gateway:BaseUrl is required.")).Host);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseBaseForwardedHeaders();
    app.UseBaseSecurityHeaders();
    app.UseBaseRequestLogging();   // outside the exception page: the response log has the status the client gets
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapBaseHealth();
    app.MapRazorPages();
});
