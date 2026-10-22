using Xyz.ServiceProduct.Host.Endpoints;
using Xyz.ServiceProduct.Host.Handlers;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared.Events;
using Xyz.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSharedWebApi("product-service")
    .AddSharedJwtAuthentication(builder.Configuration)
    .AddSharedRedis(builder.Configuration)
    .AddSharedMongo(builder.Configuration)
    .AddSharedRabbitMq(builder.Configuration)
    .AddSingleton<ProductStore>()
    .AddEventHandler<OrderCreated, OrderCreatedHandler>();

var app = builder.Build();

await app.RunStartupStepAsync("ProductStore setup", app.Services.GetRequiredService<ProductStore>().InitializeAsync);

app.UseSharedPipeline();
app.MapSharedHealth();
app.MapProductEndpoints();

await app.RunAsync();
