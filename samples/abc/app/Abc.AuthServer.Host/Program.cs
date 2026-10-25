using Abc.AuthServer.Host.Security;
using Abc.AuthServer.Host.Services;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.AuthServer");

builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.AuthServer API")
    .AddIdentityPersistence(builder.Configuration)
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

// the private key is loaded now: a missing key stops the host here, not at the first login
_ = app.Services.GetRequiredService<TokenService>();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
