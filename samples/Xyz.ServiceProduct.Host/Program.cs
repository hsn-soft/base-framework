using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;
using Xyz.ServiceProduct.Host.Endpoints;
using Xyz.ServiceProduct.Host.Handlers;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared;
using Xyz.Shared.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddXyzLogging(builder.Configuration, "Xyz.ServiceProduct");

builder.Services
    .AddXyzApi(builder.Configuration)
    .AddXyzAuthorization()
    .AddBaseMongo(builder.Configuration)
    .AddRabbitMqEventBus(builder.Configuration)
    .AddSingleton<ProductStore>()
    .AddEventHandler<OrderCreated, OrderCreatedHandler>();

WebApplication app = builder.Build();

await app.Services.RunStartupStepAsync("ProductStore setup", app.Services.GetRequiredService<ProductStore>().InitializeAsync);

app.UseXyzApi();
app.MapProductEndpoints();

await app.RunAsync();
