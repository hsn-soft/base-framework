using Abc.Shared.Hosting.Gateways;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Gateway.Commercial");
builder.WebHost.ConfigureAbcGatewayKestrel();

builder.Services.AddAbcGateway(builder.Configuration, "Abc.Gateway.Commercial API", authenticateUsers: true);

WebApplication app = builder.Build();

app.UseAbcGateway(authenticateUsers: true);

await app.RunAsync();
