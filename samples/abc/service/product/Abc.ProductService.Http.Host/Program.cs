using Abc.ProductService.Application;
using Abc.ProductService.MongoDB;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API")
    .AddAbcEventBus(builder.Configuration)
    .AddProductPersistence(builder.Configuration)
    .AddProductApplication();

WebApplication app = builder.Build();

// the unique index on the sku, and two products to try the service with when there is none
await app.Services.EnsureProductDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
