using Abc.IdentityService.Application;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.Shared.Hosting;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.IdentityService API")
    .AddAbcEventBus(builder.Configuration)
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityApplication();

WebApplication app = builder.Build();

// the schema, and two users to sign in as when there is none: admin of the system tenant, alice of Acme (the auth server reads the same database)
await app.Services.EnsureIdentityDatabaseAsync(AbcTenants.System, AbcTenants.Acme);

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
