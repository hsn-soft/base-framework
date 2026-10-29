using Abc.Shared.Hosting.Microservices;
using Abc.Shared.Localization;
using Abc.ShipmentService.Application;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using Abc.ShipmentService.EntityFrameworkCore;
using Abc.ShipmentService.Http.Host.EventHandlers;
using Abc.ShipmentService.MongoDB;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ShipmentService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Abc services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Shipment).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.ShipmentService API")
    .AddAbcEventBus(builder.Configuration)
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddShipmentPersistence(builder.Configuration)
    .AddShipmentHistoryPersistence(builder.Configuration)
    .AddShipmentApplication(builder.Configuration);

WebApplication app = builder.Build();

// the indexes of the shipments
await app.Services.EnsureShipmentDatabaseAsync();
await app.Services.EnsureShipmentHistoryDatabaseAsync();

app.UseAbcMicroservice();
app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
app.MapControllers();

await app.RunAsync();
