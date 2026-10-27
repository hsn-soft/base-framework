using Abc.OrderService.Application;
using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.EntityFrameworkCore;
using Abc.OrderService.EntityFrameworkCore.Context;
using Abc.OrderService.Http.Host.EventHandlers;
using Abc.Shared.Contracts.Events.Product;
using Abc.Shared.Hosting.Microservices;
using Abc.Shared.Localization;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.EntityFrameworkCore;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.OrderService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Abc services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Order).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.OrderService API")
    .AddAbcEventBus(builder.Configuration)
    // an event that is not named here is handled one message at a time; this one five at a time (each message changes its own order)
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { [nameof(OrderStockReservedEto)] = 5 })
    .AddOrderPersistence(builder.Configuration)
    .AddEventInbox<OrderDbContext>()
    .AddOrderApplication(builder.Configuration);

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<OrderDbContext>();

app.UseAbcMicroservice();
app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
app.MapControllers();

await app.RunAsync();
