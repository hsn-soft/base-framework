using Edx.Integration.FirmInvoice.Application;
using Edx.Integration.FirmInvoice.Application.Storage;
using Edx.Integration.FirmInvoice.Grpc.Host;
using Edx.Integration.FirmInvoice.MongoDB;
using Edx.Shared.Hosting.Microservices;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Integration.FirmInvoice.Grpc");

// the biggest file is a setting of the service (FirmInvoice:MaxUploadBytes); Kestrel listens on one HTTP/2 port (a clear-text port cannot negotiate HTTP/2 beside HTTP/1.1)
long maxUpload = FirmInvoiceOptions.MaxUploadBytesOf(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUpload + (1024 * 1024));

// the same parts as the REST host, without the event handlers: this host only queues and publishes
builder.Services
    .AddBaseRedis(builder.Configuration)
    .AddEdxMicroservice(builder.Configuration, "Edx.Integration.FirmInvoice gRPC")
    .AddEdxEventBus(builder.Configuration)
    .AddBaseTenantClaims()
    .AddEdxPermissions(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddFirmInvoicePersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddFirmInvoiceApplication(builder.Configuration)
    .AddFirmInvoiceGrpc();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxMicroservice();
    app.MapFirmInvoiceGrpc();
});
