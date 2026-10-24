using Xyz.AuthServer.Host.Services;
using Xyz.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddXyzLogging(builder.Configuration, "Xyz.AuthServer");

builder.Services
    .AddXyzApi(builder.Configuration)
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

app.UseXyzApi();
app.MapControllers();

await app.RunAsync();
