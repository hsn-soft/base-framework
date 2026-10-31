using Edx.Shared.Hosting.Microservices;
using Edx.SignerService.Http.Host.EventHandlers;
using Edx.SignerService.Http.Host.Signing;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.SignerService");

// what every Edx microservice has (logging, the web api shell, JWT, Swagger, health, Kafka), then what this service has: no database, no Redis (it has no endpoint that needs a permission), the store of the files and the tenant of the messages
builder.Services
    .AddEdxMicroservice(builder.Configuration, "Edx.SignerService API")
    .AddEdxEventBus(builder.Configuration)
    .AddBaseTenantEventBus()
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    // the store of the files: the same MinIO the invoice service writes to (the container comes with the event)
    .AddMinioBlobStoring(builder.Configuration)
    .AddSingleton(TimeProvider.System)
    .AddSingleton<IInvoiceSigner, FakeInvoiceSigner>()
    .AddScoped<InvoiceSigningService>();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxMicroservice();
    app.MapControllers();
});
