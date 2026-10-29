using Abc.Shared.Hosting.Gateways;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Gateway.Commercial");
builder.WebHost.ConfigureAbcGatewayKestrel();

builder.Services
    .AddAbcGateway(builder.Configuration, "Abc.Gateway.Commercial API", authenticateUsers: true)
    .AddBaseRedis(builder.Configuration);

WebApplication app = builder.Build();

app.UseAbcGateway(authenticateUsers: true);

await app.RunAsync();
