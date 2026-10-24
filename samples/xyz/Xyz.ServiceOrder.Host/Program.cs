using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using Xyz.ServiceOrder.Host.Handlers;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared;
using Xyz.Shared.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddXyzLogging(builder.Configuration, "Xyz.ServiceOrder");

// Everything infrastructural comes from HsnSoft.Base.*; the service only says what it uses.
builder.Services
    .AddXyzApi(builder.Configuration)
    .AddBasePostgres<OrderDbContext>(builder.Configuration)
    .AddRabbitMqEventBus(builder.Configuration)
    .AddEventHandler<StockReserved, StockReservedHandler>()
    .AddEventHandler<StockRejected, StockRejectedHandler>()
    .AddEventHandler<ShipmentDispatched, ShipmentDispatchedHandler>();

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<OrderDbContext>();

app.UseXyzApi();
app.MapControllers();

await app.RunAsync();
