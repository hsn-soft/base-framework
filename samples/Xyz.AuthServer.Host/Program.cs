using Xyz.AuthServer.Host.Endpoints;
using Xyz.AuthServer.Host.Services;
using Xyz.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddXyzApi(builder.Configuration)
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddSingleton<TokenService>();

var app = builder.Build();

app.UseXyzApi();
app.MapAuthEndpoints();

await app.RunAsync();
