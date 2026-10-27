using Edx.AuthServer.Host.Security;
using Edx.AuthServer.Host.Services;
using Edx.IdentityService.EntityFrameworkCore;
using Edx.Shared.Hosting;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.AuthServer");

// the auth server reaches no broker: it uses the shell every host has, not the microservices' one
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxHosting(builder.Configuration, "Edx.AuthServer API", authenticateUsers: true, swaggerBearerButton: true)
    .AddIdentityPersistence(builder.Configuration)
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

// the private key is loaded now: a missing key stops the host here, not at the first login
_ = app.Services.GetRequiredService<TokenService>();

app.UseEdxHosting(authenticateUsers: true);
app.MapControllers();

await app.RunAsync();
