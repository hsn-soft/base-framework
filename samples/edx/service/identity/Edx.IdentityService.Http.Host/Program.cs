using Edx.IdentityService.Application;
using Edx.IdentityService.EntityFrameworkCore;
using Edx.IdentityService.Http.Host;
using Edx.Shared.Hosting;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.IdentityService");

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.IdentityService API")
    .AddEdxAuthentication(builder.Configuration)
    .AddEdxAuthorization()
    // this service has tenants: the tenant of a request is the one of its token
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddHostedService<PermissionCacheSyncService>()
    .Configure<IdentitySeedOptions>(builder.Configuration.GetSection(IdentitySeedOptions.SectionName))
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityApplication(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxMicroservice();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.MapControllers();
});
