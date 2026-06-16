using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using MongoDB.Driver;
using Hhs.Shared.Retry;

namespace Hhs.TextNormalizerService.Services;

public sealed class NormalizerOperationAppService(
    NormalizerMongoContext context,
    IContentScraper scraper,
    IEventBus eventBus,
    ILogger<NormalizerOperationAppService> logger,
    IOutlineProviderResolver outlineProviderResolver)
{
    private readonly ILogger<NormalizerOperationAppService> _logger = logger;

    public async Task CreateCustomerNormalizeRequestAsync(
        CustomerNormalizeRequestCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var existing = await context.CustomerRequests
            .Find(x => x.SourceEventId == @event.EventId || x.CustomerContentId == @event.CustomerContentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            await eventBus.PublishAsync(new CustomerScrapingStartedEvent { ContentProcessType = ContentProcessTypes.CustomerContent, CustomerContentId = existing.CustomerContentId, CorrelationId = existing.CorrelationId }, cancellationToken);

            return;
        }

        var requestId = Guid.NewGuid();

        await context.CustomerRequests.InsertOneAsync(new CustomerContentNormalizedRequest
        {
            Id = requestId,
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            CustomerContentId = @event.CustomerContentId!.Value,
            OutlineProviderKey = @event.OutlineProviderKey,
            VideoProviderKey = @event.VideoProviderKey,
            AudioProviderKey = @event.AudioProviderKey,
            Url = @event.Url,
            Status = "CREATED",
            CurrentStep = EventNames.CustomerNormalizeRequestCreated,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        await eventBus.PublishAsync(new CustomerScrapingStartedEvent { ContentProcessType = ContentProcessTypes.CustomerContent, CustomerContentId = @event.CustomerContentId, CorrelationId = @event.CorrelationId }, cancellationToken);
    }

    public async Task StartCustomerScrapingAsync(
        CustomerScrapingStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = "SCRAPING";
            request.ScrapingStatus = "STARTED";
            request.CurrentStep = EventNames.CustomerScrapingStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            var result = await scraper.ScrapeAsync(request.Url, cancellationToken);

            request.Status = "SCRAPING_COMPLETED";
            request.ScrapingStatus = "COMPLETED";
            request.CurrentStep = EventNames.CustomerScrapingCompleted;
            request.ScrapingResult = new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc };
            request.LastError = null;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerScrapingCompletedEvent
            {
                ContentProcessType = ContentProcessTypes.CustomerContent,
                CustomerContentId = request.CustomerContentId,
                CorrelationId = @event.CorrelationId,
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerScrapingStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task CompleteCustomerScrapingAsync(
        CustomerScrapingCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new CustomerOutlineStartedEvent { ContentProcessType = ContentProcessTypes.CustomerContent, CustomerContentId = @event.CustomerContentId, CorrelationId = @event.CorrelationId }, cancellationToken);
    }

    public async Task StartCustomerOutlineAsync(
        CustomerOutlineStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = "OUTLINE_PROVIDER_REQUEST_STARTED";
            request.OutlineStatus = "STARTED";
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            if (request.ScrapingResult is null)
                throw new InvalidOperationException("ScrapingResult is required before outline.");

            await eventBus.PublishAsync(new OutlineProviderRequestStartedEvent
            {
                CustomerContentId = request.CustomerContentId,
                ContentProcessType = ContentProcessTypes.CustomerContent,
                CorrelationId = @event.CorrelationId,
                NormalizedRequestId = request.Id,
                ProviderKey = request.OutlineProviderKey,
                InputText = request.ScrapingResult.Text
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.OutlineProviderRequestStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task StartOutlineProviderRequestAsync(
        OutlineProviderRequestStartedEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = outlineProviderResolver.Resolve(@event.ProviderKey);

            var response = await provider.CreateAsync(
                new OutlineCreateRequest { InputText = @event.InputText },
                cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.Script))
                    throw new InvalidOperationException("Script is required for immediate outline provider.");

                await eventBus.PublishAsync(new OutlineProviderCompletedEvent
                {
                    CustomerContentId = @event.CustomerContentId,
                    AnalysisContentId = @event.AnalysisContentId,
                    ContentProcessType = @event.ContentProcessType,
                    CorrelationId = @event.CorrelationId,
                    NormalizedRequestId = @event.NormalizedRequestId,
                    CustomerContentIdForItem = @event.CustomerContentIdForItem,
                    SortOrder = @event.SortOrder,
                    Script = response.Script
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("ProviderTrackId is required for async outline provider.");

            await SaveOutlinePollingStateAsync(@event, response.ProviderTrackId, cancellationToken);

            await eventBus.PublishAsync(new OutlineProviderPollingStartedEvent
            {
                CustomerContentId = @event.CustomerContentId,
                AnalysisContentId = @event.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
                CorrelationId = @event.CorrelationId,
                ProviderKey = @event.ProviderKey,
                NormalizedRequestId = @event.NormalizedRequestId,
                CustomerContentIdForItem = @event.CustomerContentIdForItem,
                SortOrder = @event.SortOrder,
                ProviderTrackId = response.ProviderTrackId
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleOutlineProviderRequestExceptionAsync(@event, ex, cancellationToken);
        }
    }

    private async Task HandleOutlineProviderRequestExceptionAsync(
        OutlineProviderRequestStartedEvent @event,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (@event.ContentProcessType == ContentProcessTypes.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            await HandleCustomerExceptionAsync(
                request,
                EventNames.OutlineProviderRequestStarted,
                ex,
                cancellationToken);

            return;
        }

        var analysis = await context.AnalysisRequests
            .Find(x => x.Id == @event.NormalizedRequestId)
            .FirstAsync(cancellationToken);

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required.");

        var item = analysis.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        await HandleAnalysisItemExceptionAsync(
            analysis,
            item,
            EventNames.OutlineProviderRequestStarted,
            ex,
            cancellationToken);
    }

    private async Task SaveOutlinePollingStateAsync(
        OutlineProviderRequestStartedEvent @event,
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        if (@event.ContentProcessType == ContentProcessTypes.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            request.OutlineProviderTrackId = providerTrackId;
            request.Status = "OUTLINE_PROVIDER_POLLING";
            request.OutlineStatus = "POLLING";
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;
            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(5);
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "OUTLINE_PROVIDER_POLLING")
            .Set("Items.$.CurrentStep", EventNames.OutlineProviderPollingStarted)
            .Set("Items.$.OutlineProviderTrackId", providerTrackId)
            .Set("Items.$.OutlineStatus", "POLLING")
            .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddSeconds(5))
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.Status, "OUTLINE_PROVIDER_POLLING")
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            @event.NormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            update,
            cancellationToken);
    }

    public async Task ScheduleOutlinePollingAsync(
        OutlineProviderPollingStartedEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event.ContentProcessType == ContentProcessTypes.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            if (request.Status is "COMPLETED" or "FAILED" or "OUTLINE_COMPLETED")
                return;

            request.OutlineProviderTrackId = @event.ProviderTrackId;
            request.Status = "OUTLINE_PROVIDER_POLLING";
            request.OutlineStatus = "POLLING";
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;
            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(5);
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "OUTLINE_PROVIDER_POLLING")
            .Set("Items.$.CurrentStep", EventNames.OutlineProviderPollingStarted)
            .Set("Items.$.OutlineProviderTrackId", @event.ProviderTrackId)
            .Set("Items.$.OutlineStatus", "POLLING")
            .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddSeconds(5))
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.Status, "OUTLINE_PROVIDER_POLLING")
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            @event.NormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            update,
            cancellationToken);
    }

    public async Task CompleteOutlineProviderAsync(
        OutlineProviderCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event.ContentProcessType == ContentProcessTypes.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            if (request.Status == "COMPLETED" ||
                request.CurrentStep == EventNames.NormalizerResultPublished ||
                request.OutlineStatus == "COMPLETED")
                return;

            request.Status = "OUTLINE_COMPLETED";
            request.OutlineStatus = "COMPLETED";
            request.CurrentStep = EventNames.CustomerOutlineCompleted;
            request.OutlineResult = new OutlineResult { Script = @event.Script };
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerOutlineCompletedEvent { CustomerContentId = request.CustomerContentId, ContentProcessType = ContentProcessTypes.CustomerContent, CorrelationId = @event.CorrelationId, Script = @event.Script }, cancellationToken);

            return;
        }

        var analysis = await context.AnalysisRequests
            .Find(x => x.Id == @event.NormalizedRequestId)
            .FirstAsync(cancellationToken);

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline completion.");

        var item = analysis.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (item.Status == "OUTLINE_COMPLETED" || item.OutlineStatus == "COMPLETED")
            return;

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "OUTLINE_COMPLETED")
            .Set("Items.$.CurrentStep", EventNames.AnalysisItemOutlineCompleted)
            .Set("Items.$.OutlineStatus", "COMPLETED")
            .Set("Items.$.OutlineResult", new OutlineResult { Script = @event.Script })
            .Set("Items.$.LastError", (string?)null)
            .Set("Items.$.NextRetryAtUtc", (DateTime?)null)
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineCompleted)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            analysis.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await RecalculateAndUpdateAnalysisParentAsync(
            analysis.Id,
            EventNames.AnalysisItemOutlineCompleted,
            null,
            cancellationToken);

        await eventBus.PublishAsync(new AnalysisItemOutlineCompletedEvent
        {
            AnalysisContentId = analysis.AnalysisContentId,
            CustomerContentId = item.CustomerContentId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            CorrelationId = @event.CorrelationId,
            SortOrder = item.SortOrder,
            Script = @event.Script
        }, cancellationToken);
    }

    public async Task CompleteCustomerOutlineAsync(
        CustomerOutlineCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        var videoInput = new
        {
            type = "customer",
            customerContentId = request.CustomerContentId,
            title = request.ScrapingResult?.Title,
            script = request.OutlineResult?.Script,
            audioItems = new[] { new { customerContentId = request.CustomerContentId, sortOrder = 1, text = request.OutlineResult?.Script } }
        };

        var completeResult = await context.CustomerRequests.UpdateOneAsync(
            x =>
                x.Id == request.Id &&
                x.Status != "COMPLETED" &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            Builders<CustomerContentNormalizedRequest>.Update
                .Set(x => x.Status, "COMPLETED")
                .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (completeResult.ModifiedCount == 0)
            return;

        await eventBus.PublishAsync(new NormalizerResultPublishedEvent
            {
                NormalizeRequestId = request.Id,
                CustomerContentId = request.CustomerContentId,
                CorrelationId = @event.CorrelationId,
                ContentProcessType = ContentProcessTypes.CustomerContent,
                VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
                VideoProviderKey = request.VideoProviderKey,
                AudioProviderKey = request.AudioProviderKey
            },
            cancellationToken);
    }

    public async Task CreateAnalysisNormalizeRequestAsync(
        AnalysisNormalizeRequestCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var existing = await context.AnalysisRequests
            .Find(x => x.SourceEventId == @event.EventId || x.AnalysisContentId == @event.AnalysisContentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            foreach (var item in existing.Items.OrderBy(x => x.SortOrder))
            {
                if (item.ScrapingStatus == "COMPLETED")
                    continue;

                await eventBus.PublishAsync(new AnalysisItemScrapingStartedEvent
                {
                    ContentProcessType = ContentProcessTypes.AnalysisContent,
                    AnalysisContentId = existing.AnalysisContentId,
                    CustomerContentId = item.CustomerContentId,
                    SortOrder = item.SortOrder,
                    CorrelationId = existing.CorrelationId
                }, cancellationToken);
            }

            return;
        }

        var request = new AnalysisContentNormalizedRequest
        {
            Id = Guid.NewGuid(),
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            AnalysisContentId = @event.AnalysisContentId!.Value,
            OutlineProviderKey = @event.OutlineProviderKey,
            VideoProviderKey = @event.VideoProviderKey,
            AudioProviderKey = @event.AudioProviderKey,
            Status = "CREATED",
            CurrentStep = EventNames.AnalysisNormalizeRequestCreated,
            Items = @event.Items.Select(x => new AnalysisNormalizedItem { CustomerContentId = x.CustomerContentId, SortOrder = x.SortOrder, Url = x.Url, Path = x.Path }).ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await context.AnalysisRequests.InsertOneAsync(request, cancellationToken: cancellationToken);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            await eventBus.PublishAsync(new AnalysisItemScrapingStartedEvent
            {
                ContentProcessType = ContentProcessTypes.AnalysisContent,
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = @event.CorrelationId
            }, cancellationToken);
        }
    }

    public async Task StartAnalysisItemScrapingAsync(
        AnalysisItemScrapingStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentId);

        try
        {
            var startUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set("Items.$.Status", "SCRAPING")
                .Set("Items.$.CurrentStep", EventNames.AnalysisItemScrapingStarted)
                .Set("Items.$.ScrapingStatus", "STARTED")
                .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                .Set(x => x.Status, "SCRAPING")
                .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingStarted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                startUpdate,
                cancellationToken);

            var result = await scraper.ScrapeAsync(item.Url, cancellationToken);

            var completeUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set("Items.$.Status", "SCRAPING_COMPLETED")
                .Set("Items.$.CurrentStep", EventNames.AnalysisItemScrapingCompleted)
                .Set("Items.$.ScrapingStatus", "COMPLETED")
                .Set("Items.$.ScrapingResult", new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc })
                .Set("Items.$.LastError", (string?)null)
                .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingCompleted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                completeUpdate,
                cancellationToken);

            await eventBus.PublishAsync(new AnalysisItemScrapingCompletedEvent
            {
                ContentProcessType = ContentProcessTypes.AnalysisContent,
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = @event.CorrelationId,
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAnalysisItemExceptionAsync(
                request,
                item,
                EventNames.AnalysisItemScrapingStarted,
                ex,
                cancellationToken);
        }
    }

    public async Task CompleteAnalysisItemScrapingAsync(
        AnalysisItemScrapingCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new AnalysisItemOutlineStartedEvent
        {
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            AnalysisContentId = @event.AnalysisContentId,
            CustomerContentId = @event.CustomerContentId,
            SortOrder = @event.SortOrder,
            CorrelationId = @event.CorrelationId
        }, cancellationToken);
    }

    public async Task StartAnalysisItemOutlineAsync(
        AnalysisItemOutlineStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentId);

        try
        {
            if (item.ScrapingStatus != "COMPLETED" || item.ScrapingResult is null)
            {
                var update = Builders<AnalysisContentNormalizedRequest>.Update
                    .Set("Items.$.Status", "WAITING_RETRY")
                    .Set("Items.$.CurrentStep", EventNames.AnalysisItemOutlineStarted)
                    .Set("Items.$.OutlineStatus", "WAITING_SCRAPING")
                    .Set("Items.$.LastError", "ScrapingResult is required before outline.")
                    .Set("Items.$.NextRetryAtUtc", DateTime.UtcNow.AddSeconds(5))
                    .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                    .Set(x => x.Status, "WAITING_RETRY")
                    .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineStarted)
                    .Set(x => x.LastError, "ScrapingResult is required before outline.")
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                await UpdateAnalysisItemAsync(request.Id, item.CustomerContentId, update, cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(request.OutlineProviderKey))
                throw new InvalidOperationException("OutlineProviderKey is required.");

            var startUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set("Items.$.Status", "OUTLINE_PROVIDER_REQUEST_STARTED")
                .Set("Items.$.OutlineStatus", "STARTED")
                .Set("Items.$.CurrentStep", EventNames.AnalysisItemOutlineStarted)
                .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                .Set(x => x.Status, "OUTLINE_PROVIDER_REQUEST_STARTED")
                .Set(x => x.CurrentStep, EventNames.OutlineProviderRequestStarted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(request.Id, item.CustomerContentId, startUpdate, cancellationToken);

            await eventBus.PublishAsync(new OutlineProviderRequestStartedEvent
            {
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                CustomerContentIdForItem = item.CustomerContentId,
                ContentProcessType = ContentProcessTypes.AnalysisContent,
                CorrelationId = @event.CorrelationId,
                NormalizedRequestId = request.Id,
                ProviderKey = request.OutlineProviderKey,
                SortOrder = item.SortOrder,
                InputText = item.ScrapingResult.Text
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAnalysisItemExceptionAsync(
                request,
                item,
                EventNames.AnalysisItemOutlineStarted,
                ex,
                cancellationToken);
        }
    }

    private static string CalculateAnalysisParentStatus(
        AnalysisContentNormalizedRequest analysis)
    {
        if (analysis.Items.Any(x => x.Status == "FAILED"))
            return "FAILED";

        if (analysis.Items.Any(x => x.Status == "WAITING_RETRY"))
            return "WAITING_RETRY";

        if (analysis.Items.Any(x =>
                x.Status is "OUTLINE_PROVIDER_POLLING" ||
                x.OutlineStatus == "POLLING"))
            return "OUTLINE_PROVIDER_POLLING";

        if (analysis.Items.Any(x =>
                x.Status is "OUTLINE_PROVIDER_REQUEST_STARTED" ||
                x.OutlineStatus == "STARTED"))
            return "OUTLINE_PROVIDER_REQUEST_STARTED";

        if (analysis.Items.All(x => x.OutlineStatus == "COMPLETED"))
            return "OUTLINE_COMPLETED";

        if (analysis.Items.Any(x => x.OutlineStatus == "COMPLETED"))
            return "OUTLINE_PARTIALLY_COMPLETED";

        if (analysis.Items.Any(x => x.ScrapingStatus == "COMPLETED"))
            return "SCRAPING_PARTIALLY_COMPLETED";

        return analysis.Status;
    }

    public async Task CompleteAnalysisItemOutlineAsync(
        AnalysisItemOutlineCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);

        if (request is { Status: "COMPLETED", CurrentStep: EventNames.NormalizerResultPublished })
        {
            return;
        }

        if (request.Items.Any(x => x.Status == "FAILED"))
        {
            await UpdateAnalysisParentAsync(
                request.Id,
                "FAILED",
                EventNames.AnalysisItemOutlineCompleted,
                "One or more items failed during processing.",
                cancellationToken);
            return;
        }

        if (request.Items.Any(x => x.OutlineStatus != "COMPLETED"))
            return;

        var videoInput = new
        {
            type = "analysis",
            analysisContentId = request.AnalysisContentId,
            audioItems = request.Items
                .OrderBy(x => x.SortOrder)
                .Select(x => new { customerContentId = x.CustomerContentId, sortOrder = x.SortOrder, title = x.ScrapingResult?.Title, text = x.OutlineResult?.Script })
                .ToList()
        };

        var completeUpdate = Builders<AnalysisContentNormalizedRequest>.Update
            .Set(x => x.Status, "COMPLETED")
            .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        var completeResult = await context.AnalysisRequests.UpdateOneAsync(
            x =>
                x.Id == request.Id &&
                x.Status != "COMPLETED" &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            completeUpdate,
            cancellationToken: cancellationToken);

        if (completeResult.ModifiedCount == 0)
            return;

        await eventBus.PublishAsync(new NormalizerResultPublishedEvent
        {
            NormalizeRequestId = request.Id,
            AnalysisContentId = request.AnalysisContentId,
            CorrelationId = @event.CorrelationId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
            VideoProviderKey = request.VideoProviderKey,
            AudioProviderKey = request.AudioProviderKey
        }, cancellationToken);
    }

    public async Task CompleteCustomerScrapingManuallyAsync(
        Guid customerContentId,
        ManualScrapingInput input,
        CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == customerContentId)
            .FirstAsync(cancellationToken);

        request.ScrapingStatus = "COMPLETED";
        request.Status = "SCRAPING_COMPLETED";
        request.CurrentStep = EventNames.CustomerScrapingCompleted;
        request.ScrapingResult = new ScrapingResult { Title = input.Title, Text = input.Text, ReleaseTimeUtc = input.ReleaseTimeUtc, Source = "MANUAL" };
        request.LastError = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new CustomerScrapingCompletedEvent
        {
            ContentProcessType = ContentProcessTypes.CustomerContent,
            CustomerContentId = customerContentId,
            CorrelationId = input.CorrelationId,
            Title = input.Title,
            Text = input.Text,
            ReleaseTimeUtc = input.ReleaseTimeUtc,
            IsManual = true
        }, cancellationToken);
    }

    private Task ReplaceCustomerAsync(CustomerContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        return context.CustomerRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceAnalysisAsync(AnalysisContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        return context.AnalysisRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }


    private Task<AnalysisContentNormalizedRequest> GetAnalysisAsync(Guid analysisContentId, CancellationToken cancellationToken)
    {
        return context.AnalysisRequests
            .Find(x => x.AnalysisContentId == analysisContentId)
            .FirstAsync(cancellationToken);
    }

    private async Task FailCustomerAsync(
        CustomerContentNormalizedRequest request,
        string step,
        Exception ex,
        bool retryable,
        CancellationToken cancellationToken)
    {
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEvent
        {
            CorrelationId = request.CorrelationId,
            CustomerContentId = request.CustomerContentId,
            ContentProcessType = ContentProcessTypes.CustomerContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private async Task HandleCustomerExceptionAsync(
        CustomerContentNormalizedRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleCustomerRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailCustomerAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleCustomerRetryAsync(
        CustomerContentNormalizedRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        request.RetryCount++;

        if (request.RetryCount >= request.MaxRetryCount)
        {
            await FailCustomerAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = "WAITING_RETRY";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            RetryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEvent
        {
            CorrelationId = request.CorrelationId,
            CustomerContentId = request.CustomerContentId,
            ContentProcessType = ContentProcessTypes.CustomerContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task HandleAnalysisItemExceptionAsync(
        AnalysisContentNormalizedRequest request,
        AnalysisNormalizedItem item,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAnalysisItemRetryAsync(request, item, step, ex, cancellationToken);
            return;
        }

        await FailAnalysisItemAsync(request, item, step, ex, false, cancellationToken);
    }

    private async Task ScheduleAnalysisItemRetryAsync(
        AnalysisContentNormalizedRequest request,
        AnalysisNormalizedItem item,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        int retryCount = item.RetryCount + 1;

        if (retryCount >= item.MaxRetryCount)
        {
            await FailAnalysisItemAsync(request, item, step, ex, false, cancellationToken);
            return;
        }

        var nextRetryAtUtc = DateTime.UtcNow.Add(
            RetryDelayCalculator.Calculate(retryCount));

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "WAITING_RETRY")
            .Set("Items.$.CurrentStep", step)
            .Set("Items.$.LastError", ex.Message)
            .Set("Items.$.RetryCount", retryCount)
            .Set("Items.$.NextRetryAtUtc", nextRetryAtUtc)
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.Status, "WAITING_RETRY")
            .Set(x => x.CurrentStep, step)
            .Set(x => x.LastError, ex.Message)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        if (step == EventNames.AnalysisItemScrapingStarted)
        {
            update = update.Set("Items.$.ScrapingStatus", "WAITING_RETRY");
        }

        if (step is EventNames.AnalysisItemOutlineStarted
            or EventNames.OutlineProviderRequestStarted
            or EventNames.OutlineProviderPollingStarted)
        {
            update = update.Set("Items.$.OutlineStatus", "WAITING_RETRY");
        }

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await eventBus.PublishAsync(new StepFailedEvent
        {
            CorrelationId = request.CorrelationId,
            AnalysisContentId = request.AnalysisContentId,
            CustomerContentId = item.CustomerContentId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }


    private async Task FailAnalysisItemAsync(
        AnalysisContentNormalizedRequest request,
        AnalysisNormalizedItem item,
        string step,
        Exception ex,
        bool retryable,
        CancellationToken cancellationToken)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "FAILED")
            .Set("Items.$.CurrentStep", step)
            .Set("Items.$.LastError", ex.Message)
            .Set("Items.$.NextRetryAtUtc", (DateTime?)null)
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.Status, "FAILED")
            .Set(x => x.CurrentStep, step)
            .Set(x => x.LastError, ex.Message)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        if (step == EventNames.AnalysisItemScrapingStarted)
            update = update.Set("Items.$.ScrapingStatus", "FAILED");

        if (step is EventNames.AnalysisItemOutlineStarted
            or EventNames.OutlineProviderRequestStarted
            or EventNames.OutlineProviderPollingStarted)
            update = update.Set("Items.$.OutlineStatus", "FAILED");

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await eventBus.PublishAsync(new StepFailedEvent
        {
            CorrelationId = request.CorrelationId,
            AnalysisContentId = request.AnalysisContentId,
            CustomerContentId = item.CustomerContentId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private Task UpdateAnalysisParentAsync(
        Guid id,
        string status,
        string currentStep,
        string? lastError,
        CancellationToken cancellationToken)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set(x => x.Status, status)
            .Set(x => x.CurrentStep, currentStep)
            .Set(x => x.LastError, lastError)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        return context.AnalysisRequests.UpdateOneAsync(
            x => x.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    private Task UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i => i.CustomerContentId == customerContentId));

        return context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private async Task RecalculateAndUpdateAnalysisParentAsync(
        Guid analysisRequestId,
        string currentStep,
        string? lastError,
        CancellationToken cancellationToken)
    {
        var analysis = await context.AnalysisRequests
            .Find(x => x.Id == analysisRequestId)
            .FirstAsync(cancellationToken);

        var status = CalculateAnalysisParentStatus(analysis);

        var filter = status == "OUTLINE_COMPLETED"
            ? Builders<AnalysisContentNormalizedRequest>.Filter.And(
                Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
                Builders<AnalysisContentNormalizedRequest>.Filter.Ne(x => x.Status, "COMPLETED"))
            : Builders<AnalysisContentNormalizedRequest>.Filter.And(
                Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
                Builders<AnalysisContentNormalizedRequest>.Filter.Nin(
                    x => x.Status,
                    new[] { "COMPLETED", "OUTLINE_COMPLETED" }));

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set(x => x.Status, status)
            .Set(x => x.CurrentStep, currentStep)
            .Set(x => x.LastError, lastError)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }
}

public sealed class ManualScrapingInput
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
}