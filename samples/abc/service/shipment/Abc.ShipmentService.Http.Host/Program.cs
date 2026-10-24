using Abc.Shared.Hosting.Microservices;
using Abc.ShipmentService.Application;
using Abc.ShipmentService.MongoDB;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MongoDB;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ShipmentService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ShipmentService API")
    .AddBaseMongo(builder.Configuration)
    .AddShipmentPersistence()
    .AddShipmentApplication();

WebApplication app = builder.Build();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
