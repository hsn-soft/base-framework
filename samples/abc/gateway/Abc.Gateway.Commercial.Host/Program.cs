using Abc.Shared.Hosting.Gateways;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Gateway.Commercial");
builder.WebHost.ConfigureAbcGatewayKestrel();

builder.Services
    .AddAbcGateway(builder.Configuration, "Abc.Gateway.Commercial API", authenticateUsers: true)
    .AddBaseRedis(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseAbcGateway(authenticateUsers: true);
});
