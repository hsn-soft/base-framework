using Abc.Shared.Hosting.Gateways;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Gateway.Public");
builder.WebHost.ConfigureAbcGatewayKestrel();

builder.Services.AddAbcGateway(builder.Configuration, "Abc.Gateway.Public API", authenticateUsers: false);

WebApplication app = builder.Build();

app.UseAbcGateway(authenticateUsers: false);

await app.RunAsync();
