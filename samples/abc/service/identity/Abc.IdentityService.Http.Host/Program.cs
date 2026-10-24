using Abc.IdentityService.Application;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.IdentityService API")
    .AddBasePostgres<IdentityDbContext>(builder.Configuration)
    .AddIdentityPersistence()
    .AddIdentityApplication();

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<IdentityDbContext>();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
