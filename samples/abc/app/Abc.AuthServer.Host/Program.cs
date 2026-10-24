using Abc.AuthServer.Host.Security;
using Abc.AuthServer.Host.Services;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.AuthServer");

builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.AuthServer API")
    .AddBasePostgres<IdentityDbContext>(builder.Configuration)
    .AddIdentityPersistence()
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

// the private key is loaded now: a missing key stops the host here, not at the first login
_ = app.Services.GetRequiredService<TokenService>();

// the users are the identity service's, in its database: it is made here too, so whichever of the two starts first has it
await app.Services.EnsureIdentityDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
