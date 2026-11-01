using Edx.InvoiceService.Application;
using Edx.InvoiceService.Http.Host.EventHandlers;
using Edx.InvoiceService.MongoDB;
using Edx.Shared.Hosting;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.InvoiceService");

// what every Edx microservice has (logging, the web api shell, Swagger), then what this service chooses (health, authentication, authorization, Kafka) and has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.InvoiceService API")
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready: leave it out and the service has none
    .AddEdxAuthentication(builder.Configuration)
    .AddEdxAuthorization()
    .AddEdxEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddInvoicePersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collection of the records): a message that comes twice is handled once
    .AddEventInbox(MongoInvoiceDbProperties.CollectionPrefix + "InboxMessage")
    // the store of the files: the same MinIO the firm invoice integration writes to (the container of this service is the Storage section)
    .AddMinioBlobStoring(builder.Configuration)
    .AddInvoiceApplication(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxMicroservice();
    app.MapBaseHealth();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.MapControllers();
});
