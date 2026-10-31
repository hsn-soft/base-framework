using Edx.Integration.FirmInvoice.Application;
using Edx.Integration.FirmInvoice.Application.Storage;
using Edx.Integration.FirmInvoice.MongoDB;
using Edx.Integration.FirmInvoice.Wcf.Host;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Integration.FirmInvoice.Wcf");

// the biggest file is a setting of the service (FirmInvoice:MaxUploadBytes); WCF reads its messages synchronously, which is why this host is not the REST one
long maxUpload = FirmInvoiceOptions.MaxUploadBytesOf(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024);
    options.AllowSynchronousIO = true;
});

// the same parts as the REST host, without the event handlers: this host only queues and publishes
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.Integration.FirmInvoice WCF")
    .AddEdxEventBus(builder.Configuration)
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddFirmInvoicePersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddFirmInvoiceApplication(builder.Configuration)
    .AddFirmInvoiceWcf();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxMicroservice();
    app.UseFirmInvoiceWcf();
});
