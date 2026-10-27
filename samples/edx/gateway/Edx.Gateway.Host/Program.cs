using Edx.Shared.Hosting.Gateways;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Gateway");
builder.WebHost.ConfigureEdxGatewayKestrel();

builder.Services
    .AddEdxGateway(builder.Configuration, "Edx.Gateway API", authenticateUsers: true)
    .AddBaseRedis(builder.Configuration);

WebApplication app = builder.Build();

app.UseEdxGateway(authenticateUsers: true);

await app.RunAsync();
