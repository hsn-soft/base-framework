using Abc.ProductService.Application;
using Abc.ProductService.Http.Host.EventHandlers;
using Abc.ProductService.MongoDB;
using Abc.Shared.Hosting.Microservices;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ProductService");

// what every Abc microservice has (logging, the web api shell, JWT, Swagger, health), then what this service has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Abc services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Product).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.ProductService API")
    .AddAbcEventBus(builder.Configuration)
    // this service has tenants: the tenant of a request is the one of its token, and it travels with the messages
    .AddBaseTenantClaims()
    .AddBaseTenantEventBus()
    // ten orders are set aside at the same time: the stock is taken in one step of the database (IProductRepository), so the number at once does not change the result
    .AddEventHandlers(typeof(EventHandlersAssemblyMarker).Assembly, new Dictionary<string, ushort> { [nameof(OrderCreatedEto)] = 10 })
    .AddProductPersistence(builder.Configuration, AbcTenants.Acme)
    .AddProductStockPersistence(builder.Configuration)
    // the inbox of the service (in its database, with the collections of the products): a message that comes twice is handled once
    .AddEventInbox(MongoProductDbProperties.CollectionPrefix + "InboxMessage")
    .AddProductApplication(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseAbcMicroservice();
    app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
    app.MapControllers();
});
