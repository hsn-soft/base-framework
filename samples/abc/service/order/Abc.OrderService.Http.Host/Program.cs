using Abc.OrderService.Application;
using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.EntityFrameworkCore;
using Abc.OrderService.EntityFrameworkCore.Context;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.OrderService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    .AddAbcMicroservice(builder.Configuration, "Abc.OrderService API", typeof(Order).Assembly)
    .AddAbcEventBus(builder.Configuration)
    .AddOrderPersistence(builder.Configuration)
    .AddOrderApplication();

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<OrderDbContext>();

app.UseAbcMicroservice();
app.MapControllers();

await app.RunAsync();
