using HsnSoft.Base.Logging.Serilog;
using Xyz.AuthServer.Application;
using Xyz.AuthServer.Domain.UserDomain.Entities;
using Xyz.AuthServer.Infrastructure;
using Xyz.AuthServer.Security;
using Xyz.AuthServer.Services;
using Xyz.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.AuthServer");

// what every Xyz microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddXyzMicroservice(builder.Configuration, "Xyz.AuthServer API", typeof(User).Assembly)
    .AddXyzEventBus(builder.Configuration)
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityHistoryPersistence(builder.Configuration)
    .AddIdentityApplication()
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

// the private key is loaded now: a missing key stops the host here, not at the first login
_ = app.Services.GetRequiredService<TokenService>();

// the schema, and two users to sign in as when there is none: admin of the system tenant, alice of Acme 
await app.Services.EnsureIdentityDatabaseAsync(XyzTenants.System, XyzTenants.Acme);
await app.Services.EnsureHistoryDatabaseAsync();

app.UseXyzMicroservice();
app.MapControllers();

await app.RunAsync();
