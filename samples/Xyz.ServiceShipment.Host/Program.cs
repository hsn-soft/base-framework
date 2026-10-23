using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;
using Xyz.ServiceShipment.Host.Endpoints;
using Xyz.ServiceShipment.Host.Handlers;
using Xyz.ServiceShipment.Host.Persistence;
using Xyz.Shared;
using Xyz.Shared.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddXyzApi(builder.Configuration)
    .AddBaseMongo(builder.Configuration)
    .AddRabbitMqEventBus(builder.Configuration)
    .AddSingleton<ShipmentStore>()
    .AddEventHandler<StockReserved, StockReservedHandler>();

WebApplication app = builder.Build();

await app.Services.RunStartupStepAsync("ShipmentStore setup", app.Services.GetRequiredService<ShipmentStore>().InitializeAsync);

app.UseXyzApi();
app.MapShipmentEndpoints();

await app.RunAsync();
