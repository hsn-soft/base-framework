using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Handlers;
using Hhs.TextNormalizerService.Infrastructure;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using Hhs.TextNormalizerService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<NormalizerMongoContext>();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<NormalizerInboxStore>();
builder.Services.AddScoped<NormalizerOperationAppService>();

builder.Services.AddScoped<IContentScraper, DummyContentScraper>();
builder.Services.AddScoped<IOutlineProvider, DummyOutlineProvider>();

builder.Services.AddScoped<CustomerNormalizeRequestCreatedEventHandler>();
builder.Services.AddScoped<CustomerScrapingStartedEventHandler>();
builder.Services.AddScoped<CustomerScrapingCompletedEventHandler>();
builder.Services.AddScoped<CustomerOutlineStartedEventHandler>();
builder.Services.AddScoped<CustomerOutlineCompletedEventHandler>();

builder.Services.AddScoped<AnalysisNormalizeRequestCreatedEventHandler>();
builder.Services.AddScoped<AnalysisItemScrapingStartedEventHandler>();
builder.Services.AddScoped<AnalysisItemScrapingCompletedEventHandler>();
builder.Services.AddScoped<AnalysisItemOutlineStartedEventHandler>();
builder.Services.AddScoped<AnalysisItemOutlineCompletedEventHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerNormalizeRequestCreatedEvent, CustomerNormalizeRequestCreatedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerScrapingStartedEvent, CustomerScrapingStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerScrapingCompletedEvent, CustomerScrapingCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerOutlineStartedEvent, CustomerOutlineStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerOutlineCompletedEvent, CustomerOutlineCompletedEventHandler>>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisNormalizeRequestCreatedEvent, AnalysisNormalizeRequestCreatedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingStartedEvent, AnalysisItemScrapingStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingCompletedEvent, AnalysisItemScrapingCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineStartedEvent, AnalysisItemOutlineStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineCompletedEvent, AnalysisItemOutlineCompletedEventHandler>>();

var app = builder.Build();

app.MapPost("/admin/customer-contents/{customerContentId:guid}/scraping/complete-manual",
    async (
        Guid customerContentId,
        ManualScrapingInput input,
        NormalizerOperationAppService appService,
        CancellationToken cancellationToken) =>
    {
        await appService.CompleteCustomerScrapingManuallyAsync(customerContentId, input, cancellationToken);
        return Results.Ok();
    });

app.Run();