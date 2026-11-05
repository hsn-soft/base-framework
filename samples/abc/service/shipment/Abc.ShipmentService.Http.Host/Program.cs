using Abc.Shared.Hosting.Http;
using Abc.Shared.Hosting.Microservices;
using Abc.Shared.Localization;
using Abc.ShipmentService.Application;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using Abc.ShipmentService.EntityFrameworkCore;
using Abc.ShipmentService.Http.Host.EventHandlers;
using Abc.ShipmentService.MongoDB;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.ShipmentService");

// what every Abc microservice has (logging, the web api shell, Swagger), then what this service chooses (health, authentication, authorization, the event bus, tenants) and has
builder.Services
    // the texts in the languages of the system, outermost layer first: the framework's (always), the Abc services', this service's own
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Shipment).Assembly))
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.ShipmentService API")
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready: leave it out and the service has none
    .AddAbcAuthentication(builder.Configuration)
    .AddAbcAuthorization()
    // the scheduler of the system calls the jobs of this service (JobsController) with its key: Scheduler:ApiKey, from the environment or the .env file, and the service does not start without it
    .AddBaseSchedulerAuth(builder.Configuration)
    .AddAbcEventBus(builder.Configuration, typeof(EventHandlersAssemblyMarker).Assembly)
    .AddShipmentPersistence(builder.Configuration)
    .AddShipmentHistoryPersistence(builder.Configuration)
    .AddShipmentApplication(builder.Configuration);

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseAbcMicroservice();
    app.MapBaseHealth();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
    app.MapControllers();
});
