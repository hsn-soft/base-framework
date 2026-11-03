using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Application;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore;
using Hsx.IdentityService.Http.Host;
using Hsx.Shared.Hosting;
using Hsx.Shared.Hosting.Microservices;
using Hsx.Shared.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Hsx.IdentityService");

// what every Hsx microservice has (logging, the web api shell, Swagger), then what this service chooses (health, authentication, authorization, tenants) and has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Hsx services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddHsxSharedTexts().AddEmbedded(typeof(Company).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddHsxMicroservice(builder.Configuration, "Hsx.IdentityService API")
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready: leave it out and the service has none
    .AddHsxAuthentication(builder.Configuration)
    .AddHsxAuthorization()
    .AddHsxPermissions(builder.Configuration)   // [PermissionAuthorize]: what the roles were granted is read from the store the identity service writes
                                                // the tenant of a request, the tenants under it and the scopes it may see are what the claims of its token say
    .AddBaseTenantClaims()
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityApplication(builder.Configuration)
    .AddHostedService<PermissionCacheSyncService>();   // the grants of the roles are written to the store at start and every few minutes

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseHsxMicroservice();
    app.MapBaseHealth();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
    app.MapControllers();
});
