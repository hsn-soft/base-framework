using Abc.ProductService.Application;
using Abc.ProductService.Application.Security;
using Abc.ProductService.Http.Host.Security;
using Abc.ProductService.MongoDB;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MongoDB;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API")
    .AddAbcEventBus(builder.Configuration)
    .AddBaseMongo(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<ICurrentUser, HttpContextCurrentUser>()
    .AddProductPersistence()
    .AddProductApplication();

WebApplication app = builder.Build();

// the unique index on the sku, and two products to try the service with when there is none
await app.Services.EnsureProductDatabaseAsync();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
