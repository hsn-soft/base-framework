using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Hhs.Shared.Providers;
using Hhs.Shared.Retry;
using Hhs.TextNormalizerService.Configuration;
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

var outlineFastProviderSettings = builder.Configuration.GetSection(OutlineFastProviderSettings.SectionName)
    .Get<OutlineFastProviderSettings>() ?? new OutlineFastProviderSettings();
builder.Services.AddSingleton(outlineFastProviderSettings);

var outlineQueueProviderSettings = builder.Configuration.GetSection(OutlineQueueProviderSettings.SectionName)
    .Get<OutlineQueueProviderSettings>() ?? new OutlineQueueProviderSettings();
builder.Services.AddSingleton(outlineQueueProviderSettings);

var outlinePollingSettings = builder.Configuration.GetSection(OutlinePollingSettings.SectionName)
    .Get<OutlinePollingSettings>() ?? new OutlinePollingSettings();
builder.Services.AddSingleton(outlinePollingSettings);

var normalizerRetrySettings = builder.Configuration.GetSection(nameof(NormalizerRetrySettings))
    .Get<NormalizerRetrySettings>() ?? new NormalizerRetrySettings();
builder.Services.AddSingleton(normalizerRetrySettings);
builder.Services.AddSingleton(_ => new RetryDelayCalculator(normalizerRetrySettings.DelaySeconds));

// CDN Provider Resolver
builder.Services.AddSingleton<ICdnProviderResolver>(sp =>
{
    var cdnProviders = new Dictionary<string, CdnProviderSettingsBase>(StringComparer.OrdinalIgnoreCase);
    var cdnSection = builder.Configuration.GetSection("Provider:Cdn");

    if (cdnSection.Exists())
    {
        foreach (var child in cdnSection.GetChildren())
        {
            var settings = child.Get<CdnProviderSettingsBase>();
            if (settings != null)
            {
                cdnProviders[child.Key] = settings;
            }
        }
    }

    return new CdnProviderResolver(cdnProviders);
});

// CDN Storage Provider Factory
builder.Services.AddSingleton<CdnStorageProviderFactory>();

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<NormalizerMongoContext>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<NormalizerInboxStore>();
builder.Services.AddScoped<NormalizerOperationAppService>();

builder.Services.AddScoped<IContentScraper, DummyContentScraper>();

builder.Services.AddScoped<IOutlineProvider, OutlineFastProvider>();
builder.Services.AddScoped<IOutlineProvider, OutlineQueueProvider>();
builder.Services.AddScoped<IOutlineProviderResolver, OutlineProviderResolver>();

builder.Services.AddScoped<CustomerContentCreatedEtoHandler>();
builder.Services.AddScoped<CustomerContentNormalizeRequestCreatedEtoHandler>();
builder.Services.AddScoped<CustomerContentScrapingStartedEtoHandler>();
builder.Services.AddScoped<CustomerContentScrapingCompletedEtoHandler>();
builder.Services.AddScoped<CustomerContentOutlineStartedEtoHandler>();
builder.Services.AddScoped<CustomerContentOutlineCompletedEtoHandler>();

builder.Services.AddScoped<AnalysisContentCreatedEtoHandler>();
builder.Services.AddScoped<AnalysisContentNormalizeRequestCreatedEtoHandler>();
builder.Services.AddScoped<AnalysisItemScrapingStartedEtoHandler>();
builder.Services.AddScoped<AnalysisItemScrapingCompletedEtoHandler>();
builder.Services.AddScoped<AnalysisItemOutlineStartedEtoHandler>();
builder.Services.AddScoped<AnalysisItemOutlineCompletedEtoHandler>();

builder.Services.AddScoped<OutlineProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<OutlineProviderCompletedEtoHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderRequestStartedEto, OutlineProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderCompletedEto, OutlineProviderCompletedEtoHandler>>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentCreatedEto, CustomerContentCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentNormalizeRequestCreatedEto, CustomerContentNormalizeRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentScrapingStartedEto, CustomerContentScrapingStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentScrapingCompletedEto, CustomerContentScrapingCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentOutlineStartedEto, CustomerContentOutlineStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentOutlineCompletedEto, CustomerContentOutlineCompletedEtoHandler>>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisContentCreatedEto, AnalysisContentCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisContentNormalizeRequestCreatedEto, AnalysisContentNormalizeRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingStartedEto, AnalysisItemScrapingStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingCompletedEto, AnalysisItemScrapingCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineStartedEto, AnalysisItemOutlineStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineCompletedEto, AnalysisItemOutlineCompletedEtoHandler>>();

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

        int processedCount = 0;

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
                    .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow.AddSeconds(5))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

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
                    .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddSeconds(5))
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

                processedCount++;
            }
        }

        return Results.Ok(new { processed = processedCount });
    });

app.Run();