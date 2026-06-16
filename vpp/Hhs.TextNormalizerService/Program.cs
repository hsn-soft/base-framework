using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Handlers;
using Hhs.TextNormalizerService.Infrastructure;
using Hhs.TextNormalizerService.Models;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using Hhs.TextNormalizerService.Services;
using Hhs.TextNormalizerService.Workers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<NormalizerMongoContext>();

builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<NormalizerInboxStore>();
builder.Services.AddScoped<NormalizerOperationAppService>();

builder.Services.AddScoped<IContentScraper, DummyContentScraper>();
builder.Services.AddScoped<IOutlineProvider, OpenAiOutlineProvider>();
builder.Services.AddScoped<IOutlineProvider, CustomXyzOutlineProvider>();
builder.Services.AddSingleton<OutlineProviderAbc>();
builder.Services.AddScoped<IOutlineProvider>(sp => sp.GetRequiredService<OutlineProviderAbc>());
builder.Services.AddSingleton<OutlineProviderXyz>();
builder.Services.AddScoped<IOutlineProvider>(sp => sp.GetRequiredService<OutlineProviderXyz>());
builder.Services.AddScoped<IOutlineProviderResolver, OutlineProviderResolver>();

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

builder.Services.AddScoped<OutlineProviderRequestStartedEventHandler>();
builder.Services.AddScoped<OutlineProviderPollingStartedEventHandler>();
builder.Services.AddScoped<OutlineProviderCompletedEventHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderRequestStartedEvent, OutlineProviderRequestStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderPollingStartedEvent, OutlineProviderPollingStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderCompletedEvent, OutlineProviderCompletedEventHandler>>();

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

builder.Services.AddScoped<OutlineProviderPollingAppService>();
builder.Services.AddHostedService<OutlineProviderPollingWorker>();

builder.Services.AddScoped<NormalizerRetryAppService>();
builder.Services.AddHostedService<NormalizerRetryWorker>();


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

app.MapPost("/demo/outline/abc",
    async (
        DemoOutlineRequest request,
        OutlineProviderAbc providerAbc,
        CancellationToken cancellationToken) =>
    {
        var result = await providerAbc.CreateAsync(
            new OutlineCreateRequest { InputText = request.Text },
            cancellationToken);
        return Results.Ok(new { provider = "outline-abc", script = result.Script, delayMs = 5000 });
    });

app.MapPost("/demo/outline/xyz",
    async (
        DemoOutlineRequest request,
        OutlineProviderXyz providerXyz,
        CancellationToken cancellationToken) =>
    {
        var result = await providerXyz.CreateAsync(
            new OutlineCreateRequest { InputText = request.Text },
            cancellationToken);
        return Results.Ok(new { provider = "outline-xyz", trackingId = result.ProviderTrackId, pollingWindowSec = 30 });
    });

app.MapGet("/demo/outline/xyz/{trackingId}",
    async (
        string trackingId,
        OutlineProviderXyz providerXyz,
        CancellationToken cancellationToken) =>
    {
        var status = await providerXyz.GetStatusAsync(trackingId, cancellationToken);
        return Results.Ok(new
        {
            provider = "outline-xyz",
            trackingId = trackingId,
            isCompleted = status.IsCompleted,
            isFailed = status.IsFailed,
            script = status.Script,
            errorMessage = status.ErrorMessage
        });
    });

app.MapGet("/demo/outline/xyz/store/list",
    () =>
    {
        var store = OutlineProviderXyz.GetStore();
        return Results.Ok(new
        {
            count = store.Count,
            entries = store.Values.Select(e => new
            {
                e.TrackingId,
                e.CreatedAtUtc,
                elapsedSeconds = (DateTime.UtcNow - e.CreatedAtUtc).TotalSeconds,
                hasResult = e.Result != null,
                e.Result
            }).ToList()
        });
    });

app.MapPost("/demo/outline/xyz/store/clear",
    () =>
    {
        OutlineProviderXyz.ClearStore();
        return Results.Ok(new { message = "Store cleared." });
    });


app.MapPost("/scheduler/outline-polling",
    async (
        NormalizerMongoContext mongoContext,
        IEventBus eventBus,
        IOutlineProviderResolver outlineProviderResolver,
        CancellationToken cancellationToken) =>
    {
        var now = DateTime.UtcNow;

        var customerRequests = await mongoContext.CustomerRequests
            .Find(x =>
                x.Status == "OUTLINE_PROVIDER_POLLING" &&
                x.NextOutlinePollAtUtc != null &&
                x.NextOutlinePollAtUtc <= now &&
                x.OutlineProviderTrackId != null)
            .Limit(10)
            .ToListAsync(cancellationToken);

        var processedCount = 0;

        foreach (var request in customerRequests)
        {
            var claimResult = await mongoContext.CustomerRequests.UpdateOneAsync(
                x =>
                    x.Id == request.Id &&
                    x.Status == "OUTLINE_PROVIDER_POLLING" &&
                    x.NextOutlinePollAtUtc != null &&
                    x.NextOutlinePollAtUtc <= now &&
                    x.OutlineProviderTrackId != null,
                Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow.AddMinutes(1))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

            await eventBus.PublishAsync(new OutlineProviderPollingStartedEvent
            {
                ProviderKey = request.OutlineProviderKey,
                NormalizedRequestId = request.Id,
                ProviderTrackId = request.OutlineProviderTrackId
            }, cancellationToken);

            processedCount++;
        }

        var analysisRequests = await mongoContext.AnalysisRequests
            .Find(x =>
                x.Items.Any(i =>
                    i.OutlineStatus == "POLLING" &&
                    i.NextOutlinePollAtUtc != null &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null))
            .Limit(10)
            .ToListAsync(cancellationToken);

        foreach (var request in analysisRequests)
        {
            var pollingItems = request.Items
                .Where(i =>
                    i.OutlineStatus == "POLLING" &&
                    i.NextOutlinePollAtUtc != null &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null)
                .OrderBy(i => i.SortOrder)
                .Take(10)
                .ToList();

            foreach (var item in pollingItems)
            {
                var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                    .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddMinutes(1))
                    .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                var claimResult = await mongoContext.AnalysisRequests.UpdateOneAsync(
                    Builders<AnalysisContentNormalizedRequest>.Filter.And(
                        Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, request.Id),
                        Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                            x => x.Items,
                            i =>
                                i.CustomerContentId == item.CustomerContentId &&
                                i.OutlineStatus == "POLLING" &&
                                i.NextOutlinePollAtUtc != null &&
                                i.NextOutlinePollAtUtc <= now &&
                                i.OutlineProviderTrackId != null)),
                    claimUpdate,
                    cancellationToken: cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                await eventBus.PublishAsync(new OutlineProviderPollingStartedEvent
                {
                    ProviderKey = request.OutlineProviderKey,
                    NormalizedRequestId = request.Id,
                    CustomerContentIdForItem = item.CustomerContentId,
                    SortOrder = item.SortOrder,
                    ProviderTrackId = item.OutlineProviderTrackId
                }, cancellationToken);

                processedCount++;
            }
        }

        return Results.Ok(new { processed = processedCount });
    });

app.Run();