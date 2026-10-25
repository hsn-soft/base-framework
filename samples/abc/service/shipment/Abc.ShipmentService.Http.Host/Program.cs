using Abc.Shared.Hosting.Microservices;
using Abc.ShipmentService.Application;
using Abc.ShipmentService.MongoDB;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ShipmentService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ShipmentService API")
    .AddAbcEventBus(builder.Configuration)
    .AddShipmentPersistence(builder.Configuration)
    .AddShipmentApplication();

WebApplication app = builder.Build();

// the indexes of the shipments
await app.Services.EnsureShipmentDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
