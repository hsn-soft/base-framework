using Abc.IdentityService.Application;
using Abc.IdentityService.Application.Security;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.IdentityService.Http.Host.Security;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.IdentityService API")
    .AddBasePostgres<IdentityDbContext>(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<ICurrentUser, HttpContextCurrentUser>()
    .AddIdentityPersistence()
    .AddIdentityApplication();

WebApplication app = builder.Build();

// the schema, and two users to sign in as when there is none (the auth server reads the same database)
await app.Services.EnsureIdentityDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
