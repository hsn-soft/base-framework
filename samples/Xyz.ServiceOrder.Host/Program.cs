using Xyz.ServiceOrder.Host.Endpoints;
using Xyz.ServiceOrder.Host.Handlers;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared.Events;
using Xyz.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Everything a service needs comes from Xyz.Shared; the service only says what it uses.
builder.Services
    .AddSharedWebApi("order-service")
    .AddSharedJwtAuthentication(builder.Configuration)
    .AddSharedRedis(builder.Configuration)
    .AddSharedPostgres<OrderDbContext>(builder.Configuration)
    .AddSharedRabbitMq(builder.Configuration)
    .AddEventHandler<StockReserved, StockReservedHandler>()
    .AddEventHandler<StockRejected, StockRejectedHandler>()
    .AddEventHandler<ShipmentDispatched, ShipmentDispatchedHandler>();

var app = builder.Build();

await app.EnsureDatabaseCreatedAsync<OrderDbContext>();

app.UseSharedPipeline();
app.MapSharedHealth();
app.MapOrderEndpoints();

await app.RunAsync();
