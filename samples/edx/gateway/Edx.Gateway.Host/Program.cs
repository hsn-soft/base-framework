using Edx.Shared.Hosting;
using Edx.Shared.Hosting.Gateways;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Gateway");
builder.WebHost.ConfigureEdxGatewayKestrel();

builder.Services
    .AddEdxGateway(builder.Configuration, "Edx.Gateway API")
    .AddEdxAuthentication(builder.Configuration)
    .AddBaseRedis(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxGateway();
    app.UseBaseAuthentication();   // the user is known to the logs of the gateway; the services decide who may do what
});
