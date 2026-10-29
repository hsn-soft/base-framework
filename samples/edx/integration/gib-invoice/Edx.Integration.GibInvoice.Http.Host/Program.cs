using Edx.Integration.GibInvoice.Application;
using Edx.Integration.GibInvoice.Http.Host.EventHandlers;
using Edx.Integration.GibInvoice.Http.Host.Workers;
using Edx.Integration.GibInvoice.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Integration.GibInvoice");

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health, Kafka), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.Integration.GibInvoice API")
    .AddEdxEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddIntegrationPersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collections of the envelopes): a message that comes twice is handled once
    .AddEventInbox(MongoIntegrationDbProperties.CollectionPrefix + "InboxMessage")
    // where the files go: MinIO today (the container of this service is the Storage section)
    .AddMinioBlobStoring(builder.Configuration)
    .AddIntegrationApplication(builder.Configuration)
    .AddHostedService<OutboundSendWorker>()
    .AddHostedService<OutboundStatusWorker>()
    .AddHostedService<InboundProcessWorker>()
    .AddHostedService<InboundStatusWorker>()
    .AddHostedService<UserDirectoryWorker>();

WebApplication app = builder.Build();

// the indexes of the collections
await app.Services.EnsureIntegrationDatabaseAsync();

app.UseEdxMicroservice();
app.MapControllers();

await app.RunAsync();
