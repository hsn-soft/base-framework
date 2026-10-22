using Xyz.ServiceShipment.Host.Endpoints;
using Xyz.ServiceShipment.Host.Handlers;
using Xyz.ServiceShipment.Host.Persistence;
using Xyz.Shared.Events;
using Xyz.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSharedWebApi("shipment-service")
    .AddSharedJwtAuthentication(builder.Configuration)
    .AddSharedRedis(builder.Configuration)
    .AddSharedMongo(builder.Configuration)
    .AddSharedRabbitMq(builder.Configuration)
    .AddSingleton<ShipmentStore>()
    .AddEventHandler<StockReserved, StockReservedHandler>();

var app = builder.Build();

await app.RunStartupStepAsync("ShipmentStore setup", app.Services.GetRequiredService<ShipmentStore>().InitializeAsync);

app.UseSharedPipeline();
app.MapSharedHealth();
app.MapShipmentEndpoints();

await app.RunAsync();
