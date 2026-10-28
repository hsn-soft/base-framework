using Edx.IntakeService.Application;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Grpc.Host;
using Edx.IntakeService.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.IntakeService.Grpc");

// the biggest file is a setting of the service (Intake:MaxUploadBytes); Kestrel listens on one HTTP/2 port (a clear-text port cannot negotiate HTTP/2 beside HTTP/1.1)
long maxUpload = IntakeOptions.MaxUploadBytesOf(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024));

// the same parts as the REST host, without the event handlers: this host only queues and publishes
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.IntakeService gRPC")
    .AddEdxEventBus(builder.Configuration)
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddIntakePersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddIntakeApplication(builder.Configuration)
    .AddIntakeGrpc();

WebApplication app = builder.Build();

app.UseEdxMicroservice();
app.MapIntakeGrpc();

await app.RunAsync();
