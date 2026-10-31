using Edx.AuthServer.Host.Security;
using Edx.AuthServer.Host.Services;
using Edx.IdentityService.EntityFrameworkCore;
using Edx.Shared.Hosting;
using HsnSoft.Base.Caching.StackExchangeRedis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.AuthServer");

// the auth server reaches no broker: it uses the shell every host has, not the microservices' one
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxHosting(builder.Configuration, "Edx.AuthServer API", authenticateUsers: true, swaggerBearerButton: true)
    .AddIdentityPersistence(builder.Configuration)
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    // the private key is loaded now: a missing key stops the host here, not at the first login
    _ = app.Services.GetRequiredService<TokenService>();

    app.UseEdxHosting(authenticateUsers: true);
    app.MapControllers();
});
