using Edx.IdentityService.Application;
using Edx.IdentityService.EntityFrameworkCore;
using Edx.IdentityService.Http.Host;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.IdentityService");

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.IdentityService API")
    // this service has tenants: the tenant of a request is the one of its token
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddHostedService<PermissionCacheSyncService>()
    .Configure<IdentitySeedOptions>(builder.Configuration.GetSection(IdentitySeedOptions.SectionName))
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityApplication(builder.Configuration);

WebApplication app = builder.Build();

// the schema, the three tenants and the sample users when there are none (the auth server reads the same database)
await app.Services.EnsureIdentityDatabaseAsync();

// the permissions of the catalogue (kept in step with the code), and the starting grants of the sample roles on a new database
await app.Services.EnsurePermissionsAsync();

app.UseEdxMicroservice();
app.MapControllers();

await app.RunAsync();
