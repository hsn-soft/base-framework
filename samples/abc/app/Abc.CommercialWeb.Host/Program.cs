using Abc.CommercialWeb.Host.Services;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Logging.Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.CommercialWeb");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseHealthChecks()
    .AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/account/login";
    options.SlidingExpiration = false;   // the sign-in ends when its token does
});
builder.Services.AddAuthorization();

// every call to the commercial gateway carries the correlation id of the page being served and the token of the signed-in user
builder.Services.AddTransient<GatewayRequestHandler>();
builder.Services
    .AddHttpClient<GatewayClient>(client => client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? throw new InvalidOperationException("Gateway:BaseUrl is required.")))
    .AddHttpMessageHandler<GatewayRequestHandler>();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseBaseForwardedHeaders();
    app.UseBaseRequestLogging();   // outside the exception page: the response log has the status the client gets
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/home/error");
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapBaseHealth();
    app.MapDefaultControllerRoute();
});
