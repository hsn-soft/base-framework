using Edx.Integration.FirmInvoice.Application;
using Edx.Integration.FirmInvoice.Application.Storage;
using Edx.Integration.FirmInvoice.Http.Host.EventHandlers;
using Edx.Integration.FirmInvoice.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;
using Microsoft.AspNetCore.Http.Features;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Integration.FirmInvoice");

// the biggest file is a setting of the service (FirmInvoice:MaxUploadBytes): Kestrel and the multipart form allow it, whatever the gateway in front allows
long maxUpload = FirmInvoiceOptions.MaxUploadBytesOf(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024));
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUpload + (1024 * 1024));

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health, Kafka), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.Integration.FirmInvoice API")
    .AddEdxEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages it publishes
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddFirmInvoicePersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collections of the queue): a message that comes twice is handled once
    .AddEventInbox(MongoFirmInvoiceDbProperties.CollectionPrefix + "InboxMessage")
    // where the bytes go: MinIO today; the registration is the only thing another store changes (the container and the root path are the Storage section)
    .AddMinioBlobStoring(builder.Configuration)
    .AddFirmInvoiceApplication(builder.Configuration);

WebApplication app = builder.Build();

// the indexes of the collections
await app.Services.EnsureFirmInvoiceDatabaseAsync();

app.UseEdxMicroservice();
app.MapControllers();

await app.RunAsync();
