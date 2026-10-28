using Edx.IntakeService.Application;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.MongoDB;
using Edx.IntakeService.Wcf.Host;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.IntakeService.Wcf");

// the biggest file is a setting of the service (Intake:MaxUploadBytes); WCF reads its messages synchronously, which is why this host is not the REST one
long maxUpload = IntakeOptions.MaxUploadBytesOf(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024);
    options.AllowSynchronousIO = true;
});

// the same parts as the REST host, without the event handlers: this host only queues and publishes
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.IntakeService WCF")
    .AddEdxEventBus(builder.Configuration)
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddIntakePersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddIntakeApplication(builder.Configuration)
    .AddIntakeWcf();

WebApplication app = builder.Build();

app.UseEdxMicroservice();
app.UseIntakeWcf();

await app.RunAsync();
