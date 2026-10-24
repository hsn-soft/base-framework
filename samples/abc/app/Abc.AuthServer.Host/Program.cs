using Abc.AuthServer.Host.Options;
using Abc.AuthServer.Host.Services;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.AuthServer");

builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.AuthServer API")
    .Configure<AuthOptions>(builder.Configuration.GetSection("Auth"))
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
