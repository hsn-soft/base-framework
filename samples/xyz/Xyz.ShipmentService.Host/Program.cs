using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using Xyz.Shared.Hosting;
using Xyz.ShipmentService.Application;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;
using Xyz.ShipmentService.EventHandlers;
using Xyz.ShipmentService.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.ShipmentService");

// what every Xyz microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddXyzMicroservice(builder.Configuration, "Xyz.ShipmentService API", typeof(Shipment).Assembly)
    .AddXyzEventBus(builder.Configuration)
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
    .AddShipmentPersistence(builder.Configuration)
    .AddShipmentHistoryPersistence(builder.Configuration)
    .AddShipmentApplication();

WebApplication app = builder.Build();

// the databases and their tables
await app.Services.EnsureShipmentDatabaseAsync();
await app.Services.EnsureShipmentHistoryDatabaseAsync();

app.UseXyzMicroservice();
app.MapControllers();

await app.RunAsync();
