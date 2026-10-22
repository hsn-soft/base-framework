using Xyz.AuthServer.Host.Endpoints;
using Xyz.AuthServer.Host.Services;
using Xyz.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSharedWebApi("auth-server")
    .AddSharedJwtAuthentication(builder.Configuration)
    .AddSharedRedis(builder.Configuration)
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddSingleton<TokenService>();

var app = builder.Build();

app.UseSharedPipeline();
app.MapSharedHealth();
app.MapAuthEndpoints();

await app.RunAsync();
