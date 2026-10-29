using Edx.InvoiceService.Application;
using Edx.InvoiceService.Http.Host.EventHandlers;
using Edx.InvoiceService.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.InvoiceService");

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health, Kafka), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.InvoiceService API")
    .AddEdxEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddInvoicePersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collection of the records): a message that comes twice is handled once
    .AddEventInbox(MongoInvoiceDbProperties.CollectionPrefix + "InboxMessage")
    // the store of the files: the same MinIO the firm invoice integration writes to (the container of this service is the Storage section)
    .AddMinioBlobStoring(builder.Configuration)
    .AddInvoiceApplication(builder.Configuration);

WebApplication app = builder.Build();

// the indexes of the collection
await app.Services.EnsureInvoiceDatabaseAsync();

app.UseEdxMicroservice();
app.MapControllers();

await app.RunAsync();
