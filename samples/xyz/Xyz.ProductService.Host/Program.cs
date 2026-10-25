using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using Xyz.ProductService.Application;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.EventHandlers;
using Xyz.ProductService.Infrastructure;
using Xyz.Shared.Contracts.Events.Order;
using Xyz.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.ProductService");

// what every Xyz microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddXyzMicroservice(builder.Configuration, "Xyz.ProductService API", typeof(Product).Assembly)
    .AddXyzEventBus(builder.Configuration)
    // ten orders are set aside at the same time: the stock is taken in one step of the database (IProductRepository), so the number at once does not change the result
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { [nameof(OrderCreatedEto)] = 10 })
    .AddProductPersistence(builder.Configuration)
    .AddProductStockPersistence(builder.Configuration)
    .AddProductApplication();

WebApplication app = builder.Build();

// the indexes, and two products of Acme to try the service with when there is none
await app.Services.EnsureProductDatabaseAsync(XyzTenants.Acme);
await app.Services.EnsureProductStockDatabaseAsync();

app.UseXyzMicroservice();
app.MapControllers();

await app.RunAsync();
