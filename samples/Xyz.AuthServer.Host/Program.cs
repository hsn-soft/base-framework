using Xyz.AuthServer.Host.Endpoints;
using Xyz.AuthServer.Host.Services;
using Xyz.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddXyzApi(builder.Configuration)
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

app.UseXyzApi();
app.MapAuthEndpoints();

await app.RunAsync();
