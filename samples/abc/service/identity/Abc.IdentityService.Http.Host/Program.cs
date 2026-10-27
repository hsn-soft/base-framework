using Abc.IdentityService.Application;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.IdentityService.EntityFrameworkCore.Context;
using Abc.Shared.Helper;
using Abc.Shared.Hosting.Microservices;
using Abc.Shared.Localization;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.EntityFrameworkCore;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Abc services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(User).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.IdentityService API")
    .AddAbcEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages
    .AddBaseTenantClaims()
    .AddBaseTenantEventBus()
    .AddIdentityPersistence(builder.Configuration)
    .AddIdentityHistoryPersistence(builder.Configuration)
    .AddEventInbox<IdentityDbContext>()
    .AddIdentityApplication(builder.Configuration);

WebApplication app = builder.Build();

// the schema, and two users to sign in as when there is none: admin of the system tenant, alice of Acme (the auth server reads the same database)
await app.Services.EnsureIdentityDatabaseAsync(AbcTenants.System, AbcTenants.Acme);
await app.Services.EnsureHistoryDatabaseAsync();

app.UseAbcMicroservice();
app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
app.MapControllers();

await app.RunAsync();
