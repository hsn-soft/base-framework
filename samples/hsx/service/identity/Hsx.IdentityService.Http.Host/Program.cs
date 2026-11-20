using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Data;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;
using Hsx.IdentityService;
using Hsx.IdentityService.Application;
using Hsx.IdentityService.Application.Seeding;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore;
using Hsx.Shared.Hosting.EventBus;
using Hsx.Shared.Hosting.Http;
using Hsx.Shared.Hosting.Microservices;
using Hsx.Shared.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies, the thread pool and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Hsx.IdentityService");

// what every Hsx microservice has (logging, the web api shell, Swagger), then what this service chooses (health, authentication, authorization, tenants) and has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Hsx services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddHsxSharedTexts().AddEmbedded(typeof(Company).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddHsxMicroservice(builder.Configuration, "Hsx.IdentityService API")
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready: leave it out and the service has none
    .AddHsxAuthentication(builder.Configuration)   // the section Jwt: the issuer, the audience of this service and the public key of the auth server (a file, put there by the deployment)
    .AddHsxAuthorization()
    .AddHsxPermissions(builder.Configuration)   // [PermissionAuthorize]: what the roles were granted is read from the store this service writes, under the tenant of the token (its claims are added with it)
    .AddHsxEventBus(builder.Configuration)   // the events this service publishes (a new user, a new temporary password); it handles none
    .AddServiceEfCoreDatabaseConfiguration(builder.Configuration)   // the context and the repositories of the identity database
    .AddServiceEfCoreDatabaseStartup()   // this host owns the identity database (SeedStartupConnections): the schema and the data of the sample are prepared at start
    .AddIdentityApplication(builder.Configuration)
    .AddDataSeeder<SubscriptionsSeeder>(EfCoreDbProperties.ConnectionStringName)   // the sales of the sample are real sales, which the application knows how to make: last, after the tenants, customers and roles of the persistence
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

/// <summary>Names the host for the tests that start it (<c>WebApplicationFactory&lt;Program&gt;</c>).</summary>
public partial class Program;
