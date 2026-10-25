using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Serilog;
using Xyz.ProductService.Application;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.EventHandlers;
using Xyz.ProductService.Infrastructure;
using Xyz.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Xyz.ProductService");

// what every Xyz microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddXyzMicroservice(builder.Configuration, "Xyz.ProductService API", typeof(Product).Assembly)
    .AddXyzEventBus(builder.Configuration)
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly)
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
