using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using Xyz.OrderService.Application;
using Xyz.OrderService.Domain.OrderDomain.Entities;
using Xyz.OrderService.EventHandlers;
using Xyz.OrderService.Infrastructure;
using Xyz.OrderService.Infrastructure.Context;
using Xyz.Shared.Contracts.Events.Product;
using Xyz.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.OrderService");

// what every Xyz microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddXyzMicroservice(builder.Configuration, "Xyz.OrderService API", typeof(Order).Assembly)
    .AddXyzEventBus(builder.Configuration)
    // an event that is not named here is handled one message at a time; this one five at a time (each message changes its own order)
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { [nameof(OrderStockReservedEto)] = 5 })
    .AddOrderPersistence(builder.Configuration)
    .AddOrderApplication();

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<OrderDbContext>();

app.UseXyzMicroservice();
app.MapControllers();

await app.RunAsync();
