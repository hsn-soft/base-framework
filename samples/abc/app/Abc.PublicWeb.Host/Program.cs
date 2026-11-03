using Abc.PublicWeb.Host.Services;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Http;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.PublicWeb");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseHealthChecks()
    .AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// every call to the public gateway carries the correlation id of the page being served; the page only reads, so a POST that failed is tried again
builder.Services
    .AddHttpClient<GatewayClient>()
    .AddBaseHttpClientDefaults(builder.Configuration, "Gateway", options => options.RetryUnsafeMethods = true);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseBaseForwardedHeaders();
    app.UseBaseSecurityHeaders();
    app.UseBaseRequestLogging();   // outside the exception page: the response log has the status the client gets
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/home/error");
    }

    app.UseRouting();
    app.MapBaseHealth();
    app.MapDefaultControllerRoute();
});
