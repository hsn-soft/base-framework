using Abc.ProductService.Application;
using Abc.ProductService.MongoDB;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Logging.Serilog;
using HsnSoft.Base.MongoDB;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API")
    .AddBaseMongo(builder.Configuration)
    .AddProductPersistence()
    .AddProductApplication();

WebApplication app = builder.Build();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
