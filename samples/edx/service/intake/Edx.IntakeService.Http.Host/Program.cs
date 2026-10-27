using Edx.IntakeService.Application;
using Edx.IntakeService.Http.Host;
using Edx.IntakeService.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;
using Microsoft.AspNetCore.Http.Features;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.IntakeService");

// the biggest file is a setting of the service (Intake:MaxUploadBytes): Kestrel and the multipart form allow it, whatever the gateway in front allows. Kestrel listens on two ports (Kestrel:Endpoints): HTTP/1.1 for REST
// and WCF, HTTP/2 for gRPC (a clear-text port cannot negotiate HTTP/2). WCF reads its messages synchronously.
long maxUpload = IntakeProtocolsRegister.MaxUploadBytes(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024);
    options.AllowSynchronousIO = true;
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUpload + (1024 * 1024));

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health, Kafka), then what this service has
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.IntakeService API")
    .AddEdxEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages it publishes
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddIntakePersistence(builder.Configuration)
    // where the bytes go: MinIO today; the registration is the only thing another store changes (the container and the root path are the Storage section)
    .AddMinioBlobStoring(builder.Configuration)
    .AddIntakeApplication(builder.Configuration)
    .AddIntakeProtocols(builder.Configuration);

WebApplication app = builder.Build();

// the indexes of the collections
await app.Services.EnsureIntakeDatabaseAsync();

app.UseEdxMicroservice();
app.MapControllers();
app.UseIntakeProtocols();

await app.RunAsync();
