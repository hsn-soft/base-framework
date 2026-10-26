using Abc.ProductService.Application;
using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Http.Host.EventHandlers;
using Abc.ProductService.MongoDB;
using Abc.Shared.Contracts.Events.Order;
using Abc.Shared.Hosting;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API", typeof(Product).Assembly)
    .AddAbcEventBus(builder.Configuration)
    // ten orders are set aside at the same time: the stock is taken in one step of the database (IProductRepository), so the number at once does not change the result
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { [nameof(OrderCreatedEto)] = 10 })
    .AddProductPersistence(builder.Configuration)
    .AddProductStockPersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collections of the products): a message that comes twice is handled once
    .AddEventInbox(MongoProductDbProperties.CollectionPrefix + "InboxMessage")
    .AddProductApplication();

WebApplication app = builder.Build();

// the indexes, and two products of Acme to try the service with when there is none
await app.Services.EnsureProductDatabaseAsync(AbcTenants.Acme);
await app.Services.EnsureProductStockDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
