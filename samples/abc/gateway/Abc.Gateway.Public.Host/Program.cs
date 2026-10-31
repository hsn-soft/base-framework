using Abc.Shared.Hosting.Gateways;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Gateway.Public");
builder.WebHost.ConfigureAbcGatewayKestrel();

builder.Services.AddAbcGateway(builder.Configuration, "Abc.Gateway.Public API", authenticateUsers: false);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseAbcGateway(authenticateUsers: false);
});
