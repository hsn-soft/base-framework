using Abc.ProductService.Application;
using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.MongoDB;
using Abc.Shared.Hosting;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API", typeof(Product).Assembly)
    .AddAbcEventBus(builder.Configuration)
    .AddProductPersistence(builder.Configuration)
    .AddProductApplication();

WebApplication app = builder.Build();

// the indexes, and two products of Acme to try the service with when there is none
await app.Services.EnsureProductDatabaseAsync(AbcTenants.Acme);

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
