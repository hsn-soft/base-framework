using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using MongoDB.Driver;
using Hhs.Shared.Retry;
using Hhs.TextNormalizerService.Models;
using Hhs.TextNormalizerService.Providers.Outline;
using Hhs.TextNormalizerService.Providers.Scraping;

using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Services;

public sealed class NormalizerOperationAppService(
    NormalizerMongoContext context,
    IContentScraper scraper,
    IEventBus eventBus,
    ILogger<NormalizerOperationAppService> logger,
    IOutlineProviderResolver outlineProviderResolver,
    OutlinePollingSettings outlinePollingSettings,
    RetryDelayCalculator retryDelayCalculator)
{
    private readonly ILogger<NormalizerOperationAppService> _logger = logger;
    private readonly OutlinePollingSettings _outlinePollingSettings = outlinePollingSettings;
    private readonly RetryDelayCalculator _retryDelayCalculator = retryDelayCalculator;

    public async Task CreateCustomerContentNormalizeRequestAsync(CustomerContentCreatedEto @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"CreateCustomerContentNormalizeRequestAsync started for RefContentId: {@event.RefContentId}, EventId: {@event.EventId}");

        var existing = await context.CustomerRequests
            .Find(x => x.SourceEventId == @event.EventId || x.CustomerContentId == @event.RefContentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation($"Existing request found, publishing event");
            await eventBus.PublishAsync(new CustomerContentNormalizeRequestCreatedEto { RefContentType = ContentType.CustomerContent,
            RefContentId = existing.CustomerContentId, CorrelationId = existing.CorrelationId }, cancellationToken);

            return;
        }

        _logger.LogInformation($"Creating new request...");

        var requestId = Guid.NewGuid();

        try
        {
            _logger.LogInformation($"Inserting record into MongoDB...");
            await context.CustomerRequests.InsertOneAsync(new CustomerContentNormalizedRequest
        {
            Id = requestId,
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            ScopeKey = @event.ScopeKey,
            CustomerContentId = @event.RefContentId,
            DomainName = @event.DomainName,
            ContentKey = @event.ContentKey,
            Status = StatusNames.Created,
            CurrentStep = EventNames.CustomerContentCreated,
            LastError = null,
            // provider keys
            // scraping states
            ScrapingStatus = null,
            ScrapingResult = null,
            // outline states
            OutlineStatus = null,
            OutlineResult = null,
            // outline polling
            OutlineProviderTrackId = null,
            NextOutlinePollAtUtc = null,
            OutlinePollingCount = 0,
            MaxOutlinePollingCount = _outlinePollingSettings.MaxAttempts,
            // event retry mechanism
            RetryCount = 0,
            NextRetryAtUtc = null,
            // audit
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        }, cancellationToken: cancellationToken);

            _logger.LogInformation($"MongoDB insert successful, publishing event...");

            await eventBus.PublishAsync(new CustomerContentNormalizeRequestCreatedEto { RefContentType = ContentType.CustomerContent,
                RefContentId = @event.RefContentId, CorrelationId = @event.CorrelationId }, cancellationToken);

            _logger.LogInformation($"Event published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in CreateCustomerContentNormalizeRequestAsync: {ex.Message}");
            throw;
        }
    }

    public async Task StartCustomerContentNormalizeAsync(CustomerContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = StatusNames.Scraping;
            request.ScrapingStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentScrapingStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerContentScrapingStartedEto { RefContentType = ContentType.CustomerContent,
            RefContentId = @event.RefContentId, CorrelationId = @event.CorrelationId }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerContentScrapingStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task StartCustomerContentScrapingAsync(CustomerContentScrapingStartedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
            .FirstAsync(cancellationToken);

        try
        {
            var result = await scraper.ScrapeAsync((request.DomainName + request.ContentKey), cancellationToken);

            request.Status = StatusNames.ScrapingCompleted;
            request.ScrapingStatus = StatusNames.Completed;
            request.CurrentStep = EventNames.CustomerContentScrapingCompleted;
            request.ScrapingResult = new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc };
            request.LastError = null;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerContentScrapingCompletedEto
            {
                RefContentType = ContentType.CustomerContent,
            RefContentId = request.CustomerContentId,
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
                EventNames.CustomerContentScrapingCompleted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task CompleteCustomerContentScrapingAsync(CustomerContentScrapingCompletedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.OutlineStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentOutlineStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerContentOutlineStartedEto { RefContentType = ContentType.CustomerContent,
            RefContentId = @event.RefContentId, CorrelationId = @event.CorrelationId }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerContentOutlineStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task StartCustomerContentOutlineAsync(CustomerContentOutlineStartedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = StatusNames.OutlineProviderRequestStarted;
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            if (request.ScrapingResult is null)
                throw new InvalidOperationException("ScrapingResult is required before outline.");

            _logger.LogInformation($"Publishing OutlineProviderRequestStartedEto with ScopeKey='{request.ScopeKey}' (null={request.ScopeKey == null})");

            await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
            {
                RefContentId = request.CustomerContentId,
                RefContentType = ContentType.CustomerContent,
                CorrelationId = @event.CorrelationId,
                NormalizedRequestId = request.Id,
                ScopeKey = request.ScopeKey,
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

    public async Task StartOutlineProviderRequestAsync(OutlineProviderRequestStartedEto @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"StartOutlineProviderRequestAsync - Event ScopeKey: '{@event.ScopeKey}' (null={@event.ScopeKey == null}, empty={string.IsNullOrWhiteSpace(@event.ScopeKey)})");

        try
        {
            var providerKey = SubscriptionScopeRegistry.GetOutlineProviderKey(@event.ScopeKey);
            _logger.LogInformation($"Got providerKey from registry: '{providerKey}' (null={providerKey == null})");

            var provider = outlineProviderResolver.Resolve(providerKey);
            _logger.LogInformation($"✓ Resolved provider successfully");

            var response = await provider.CreateAsync(new OutlineCreateRequest { OutlineInput = @event.InputText }, cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.OutlinedData))
                    throw new InvalidOperationException("Script is required for immediate outline provider.");

                await eventBus.PublishAsync(new OutlineProviderCompletedEto
                {
                    RefContentId = @event.RefContentId,
                    CorrelationId = @event.CorrelationId,
                    NormalizedRequestId = @event.NormalizedRequestId,
                    CustomerContentIdForItem = @event.RefContentIdForItem,
                    SortOrder = @event.SortOrder,
                    Script = response.OutlinedData
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("ProviderTrackId is required for async outline provider.");

            await SaveOutlinePollingStateAsync(@event, response.ProviderTrackId, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleOutlineProviderRequestExceptionAsync(@event, ex, cancellationToken);
        }
    }
    private async Task SaveOutlinePollingStateAsync(OutlineProviderRequestStartedEto @event, string providerTrackId, CancellationToken cancellationToken)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            request.OutlineProviderTrackId = providerTrackId;
            request.Status = StatusNames.OutlineProviderPolling;
            request.OutlineStatus = StatusNames.Polling;
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;
            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(_outlinePollingSettings.IntervalSeconds);
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderPolling)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineProviderTrackId)}", providerTrackId)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Polling)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(_outlinePollingSettings.IntervalSeconds))
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
            .Set(x => x.Status, StatusNames.OutlineProviderPolling)
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            @event.NormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            update,
            cancellationToken);
    }

    private async Task HandleOutlineProviderRequestExceptionAsync(OutlineProviderRequestStartedEto @event, Exception ex, CancellationToken cancellationToken)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
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
 
    public async Task CompleteOutlineProviderAsync(OutlineProviderCompletedEto @event, CancellationToken cancellationToken)
    {
        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var request = await context.CustomerRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync(cancellationToken);

            if (request.Status == StatusNames.Completed ||
                request.CurrentStep == EventNames.NormalizerResultPublished ||
                request.OutlineStatus == StatusNames.Completed)
                return;

            request.Status = StatusNames.OutlineCompleted;
            request.OutlineStatus = StatusNames.Completed;
            request.CurrentStep = EventNames.CustomerContentOutlineCompleted;
            request.OutlineResult = new OutlineResult { Script = @event.Script };
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await eventBus.PublishAsync(new CustomerContentOutlineCompletedEto { RefContentId = request.CustomerContentId, RefContentType = ContentType.CustomerContent, CorrelationId = @event.CorrelationId, Script = @event.Script }, cancellationToken);

            return;
        }

        var analysis = await context.AnalysisRequests
            .Find(x => x.Id == @event.NormalizedRequestId)
            .FirstAsync(cancellationToken);

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline completion.");

        var item = analysis.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (item.Status == StatusNames.OutlineCompleted || item.OutlineStatus == StatusNames.Completed)
            return;

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineCompleted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineCompleted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Completed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", new OutlineResult { Script = @event.Script })
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
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

        await eventBus.PublishAsync(new AnalysisItemOutlineCompletedEto
        {
            RefContentId = analysis.AnalysisContentId,
            RefContentIdForItem = item.CustomerContentId,
            RefContentType = ContentType.AnalysisContent,
            CorrelationId = @event.CorrelationId,
            SortOrder = item.SortOrder,
            Script = @event.Script
        }, cancellationToken);
    }

    public async Task CompleteCustomerContentOutlineAsync(CustomerContentOutlineCompletedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
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
                x.Status != StatusNames.Completed &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            Builders<CustomerContentNormalizedRequest>.Update
                .Set(x => x.Status, StatusNames.Completed)
                .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (completeResult.ModifiedCount == 0)
            return;

        await eventBus.PublishAsync(new NormalizerResultPublishedEto
            {
                NormalizeRequestId = request.Id,
                RefContentId = request.CustomerContentId,
                CorrelationId = @event.CorrelationId,
                RefContentType = ContentType.CustomerContent,
                VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
            },
            cancellationToken);
    }


    public async Task CreateAnalysisContentNormalizeRequestAsync(AnalysisContentCreatedEto @event, CancellationToken cancellationToken)
    {
        var existing = await context.AnalysisRequests
            .Find(x => x.SourceEventId == @event.EventId || x.AnalysisContentId == @event.RefContentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            await eventBus.PublishAsync(new AnalysisContentNormalizeRequestCreatedEto { RefContentType = ContentType.AnalysisContent,
            RefContentId = existing.AnalysisContentId, CorrelationId = existing.CorrelationId }, cancellationToken);

            return;
        }

        var request = new AnalysisContentNormalizedRequest
        {
            Id = Guid.NewGuid(),
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            ScopeKey = @event.ScopeKey,
            DomainName = @event.DomainName,
            AnalysisContentId = @event.RefContentId,
            Status = StatusNames.Created,
            CurrentStep = EventNames.AnalysisContentCreated,
            Items = @event.Items.Select(x => new AnalysisNormalizedItem 
            { 
                CustomerContentId = x.CustomerContentId, 
                SortOrder = x.SortOrder, 
                ContentKey = x.ContentKey 
            }).ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await context.AnalysisRequests.InsertOneAsync(request, cancellationToken: cancellationToken);

        await eventBus.PublishAsync(new AnalysisContentNormalizeRequestCreatedEto { RefContentType = ContentType.AnalysisContent,
            RefContentId = @event.RefContentId, CorrelationId = @event.CorrelationId }, cancellationToken);
    }


    public async Task StartAnalysisContentNormalizeAsync(AnalysisContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken)
    {
        var request = await context.AnalysisRequests
            .Find(x => x.AnalysisContentId == @event.RefContentId)
            .FirstAsync(cancellationToken);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            if (item.ScrapingStatus == StatusNames.Completed)
                continue;

            await eventBus.PublishAsync(new AnalysisItemScrapingStartedEto
            {
                RefContentType = ContentType.AnalysisContent,
                RefContentId = request.AnalysisContentId,
                RefContentIdForItem = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = request.CorrelationId
            }, cancellationToken);
        }
    }


    public async Task StartAnalysisItemScrapingAsync(AnalysisItemScrapingStartedEto @event, CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.RefContentId, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.RefContentId);

        try
        {
            var startUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Scraping)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Started)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                .Set(x => x.Status, StatusNames.Scraping)
                .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingStarted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                startUpdate,
                cancellationToken);

            var result = await scraper.ScrapeAsync((request.DomainName + item.ContentKey), cancellationToken);

            var completeUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.ScrapingCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Completed)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingResult)}", new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc })
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingCompleted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                completeUpdate,
                cancellationToken);

            await eventBus.PublishAsync(new AnalysisItemScrapingCompletedEto
            {
                RefContentType = ContentType.AnalysisContent,
                RefContentId = request.AnalysisContentId,
                RefContentIdForItem = item.CustomerContentId,
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

    public async Task CompleteAnalysisItemScrapingAsync(AnalysisItemScrapingCompletedEto @event, CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new AnalysisItemOutlineStartedEto
        {
            RefContentType = ContentType.AnalysisContent,
            RefContentId = @event.RefContentId,
            RefContentIdForItem = @event.RefContentIdForItem,
            SortOrder = @event.SortOrder,
            CorrelationId = @event.CorrelationId
        }, cancellationToken);
    }

    public async Task StartAnalysisItemOutlineAsync(AnalysisItemOutlineStartedEto @event, CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.RefContentId, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.RefContentId);

        try
        {
            if (item.ScrapingStatus != StatusNames.Completed || item.ScrapingResult is null)
            {
                var update = Builders<AnalysisContentNormalizedRequest>.Update
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.WaitingScraping)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", "ScrapingResult is required before outline.")
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(_outlinePollingSettings.ErrorRescheduleDelaySeconds))
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                    .Set(x => x.Status, StatusNames.WaitingRetry)
                    .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineStarted)
                    .Set(x => x.LastError, "ScrapingResult is required before outline.")
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                await UpdateAnalysisItemAsync(request.Id, item.CustomerContentId, update, cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey)))
                throw new InvalidOperationException("OutlineProviderKey is required.");

            var startUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderRequestStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Started)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                .Set(x => x.Status, StatusNames.OutlineProviderRequestStarted)
                .Set(x => x.CurrentStep, EventNames.OutlineProviderRequestStarted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(request.Id, item.CustomerContentId, startUpdate, cancellationToken);

            await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
            {

                RefContentId = item.CustomerContentId,
                CustomerContentIdForItem = item.CustomerContentId,
                RefContentType = ContentType.AnalysisContent,
                CorrelationId = @event.CorrelationId,
                NormalizedRequestId = request.Id,
                ScopeKey = request.ScopeKey,
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

    private static string CalculateAnalysisParentStatus(AnalysisContentNormalizedRequest analysis)
    {
        if (analysis.Items.Any(x => x.Status == StatusNames.Failed))
            return StatusNames.Failed;

        if (analysis.Items.Any(x => x.Status == StatusNames.WaitingRetry))
            return StatusNames.WaitingRetry;

        if (analysis.Items.Any(x =>
                x.Status is StatusNames.OutlineProviderPolling ||
                x.OutlineStatus == StatusNames.Polling))
            return StatusNames.OutlineProviderPolling;

        if (analysis.Items.Any(x =>
                x.Status is StatusNames.OutlineProviderRequestStarted ||
                x.OutlineStatus == StatusNames.Started))
            return StatusNames.OutlineProviderRequestStarted;

        if (analysis.Items.All(x => x.OutlineStatus == StatusNames.Completed))
            return StatusNames.OutlineCompleted;

        if (analysis.Items.Any(x => x.OutlineStatus == StatusNames.Completed))
            return StatusNames.OutlinePartiallyCompleted;

        if (analysis.Items.Any(x => x.ScrapingStatus == StatusNames.Completed))
            return StatusNames.ScrapingPartiallyCompleted;

        return analysis.Status;
    }

    public async Task CompleteAnalysisItemOutlineAsync(AnalysisItemOutlineCompletedEto @event, CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.RefContentId, cancellationToken);

        if (request is { Status: StatusNames.Completed, CurrentStep: EventNames.NormalizerResultPublished })
        {
            return;
        }

        if (request.Items.Any(x => x.Status == StatusNames.Failed))
        {
            await UpdateAnalysisParentAsync(
                request.Id,
                StatusNames.Failed,
                EventNames.AnalysisItemOutlineCompleted,
                "One or more items failed during processing.",
                cancellationToken);
            return;
        }

        if (request.Items.Any(x => x.OutlineStatus != StatusNames.Completed))
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
            .Set(x => x.Status, StatusNames.Completed)
            .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        var completeResult = await context.AnalysisRequests.UpdateOneAsync(
            x =>
                x.Id == request.Id &&
                x.Status != StatusNames.Completed &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            completeUpdate,
            cancellationToken: cancellationToken);

        if (completeResult.ModifiedCount == 0)
            return;

        await eventBus.PublishAsync(new NormalizerResultPublishedEto
        {
            NormalizeRequestId = request.Id,
            RefContentId = request.AnalysisContentId,
            CorrelationId = @event.CorrelationId,
            RefContentType = ContentType.AnalysisContent,
            VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
        }, cancellationToken);
    }

    public async Task CompleteCustomerScrapingManuallyAsync(Guid customerContentId, ManualScrapingInput input, CancellationToken cancellationToken)
    {
        var request = await context.CustomerRequests
            .Find(x => x.CustomerContentId == customerContentId)
            .FirstAsync(cancellationToken);

        request.ScrapingStatus = StatusNames.Completed;
        request.Status = StatusNames.ScrapingCompleted;
        request.CurrentStep = EventNames.CustomerContentScrapingCompleted;
        request.ScrapingResult = new ScrapingResult { Title = input.Title, Text = input.Text, ReleaseTimeUtc = input.ReleaseTimeUtc, Source = "MANUAL" };
        request.LastError = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new CustomerContentScrapingCompletedEto
        {
            RefContentType = ContentType.CustomerContent,
            RefContentId = customerContentId,
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

    private Task<AnalysisContentNormalizedRequest> GetAnalysisAsync(Guid analysisContentId, CancellationToken cancellationToken)
    {
        return context.AnalysisRequests
            .Find(x => x.AnalysisContentId == analysisContentId)
            .FirstAsync(cancellationToken);
    }

    private async Task FailCustomerAsync(CustomerContentNormalizedRequest request, string step, Exception ex, bool retryable, CancellationToken cancellationToken)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CorrelationId = request.CorrelationId,
            RefContentId = request.CustomerContentId,
            RefContentType = ContentType.CustomerContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private async Task HandleCustomerExceptionAsync(CustomerContentNormalizedRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleCustomerRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailCustomerAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleCustomerRetryAsync(CustomerContentNormalizedRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        request.RetryCount++;

        if (request.RetryCount >= request.MaxRetryCount)
        {
            await FailCustomerAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CorrelationId = request.CorrelationId,
            RefContentId = request.CustomerContentId,
            RefContentType = ContentType.CustomerContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task HandleAnalysisItemExceptionAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex, CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAnalysisItemRetryAsync(request, item, step, ex, cancellationToken);
            return;
        }

        await FailAnalysisItemAsync(request, item, step, ex, false, cancellationToken);
    }

    private async Task ScheduleAnalysisItemRetryAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex, CancellationToken cancellationToken)
    {
        int retryCount = item.RetryCount + 1;

        if (retryCount >= item.MaxRetryCount)
        {
            await FailAnalysisItemAsync(request, item, step, ex, false, cancellationToken);
            return;
        }

        var nextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(retryCount));

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.RetryCount)}", retryCount)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", nextRetryAtUtc)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
            .Set(x => x.Status, StatusNames.WaitingRetry)
            .Set(x => x.CurrentStep, step)
            .Set(x => x.LastError, ex.Message)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        if (step == EventNames.AnalysisItemScrapingStarted)
        {
            update = update.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.WaitingRetry);
        }

        if (step is EventNames.AnalysisItemOutlineStarted
            or EventNames.OutlineProviderRequestStarted
            or EventNames.OutlineProviderPollingStarted)
        {
            update = update.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.WaitingRetry);
        }

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CorrelationId = request.CorrelationId,
            RefContentId = item.CustomerContentId,
            RefContentType = ContentType.AnalysisContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task FailAnalysisItemAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex, bool retryable, CancellationToken cancellationToken)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Failed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
            .Set(x => x.Status, StatusNames.Failed)
            .Set(x => x.CurrentStep, step)
            .Set(x => x.LastError, ex.Message)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        if (step == EventNames.AnalysisItemScrapingStarted)
            update = update.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Failed);

        if (step is EventNames.AnalysisItemOutlineStarted
            or EventNames.OutlineProviderRequestStarted
            or EventNames.OutlineProviderPollingStarted)
            update = update.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Failed);

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CorrelationId = request.CorrelationId,
            RefContentId = item.CustomerContentId,
            RefContentType = ContentType.AnalysisContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private Task UpdateAnalysisParentAsync(Guid id, string status, string currentStep, string? lastError, CancellationToken cancellationToken)
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

    private Task UpdateAnalysisItemAsync(Guid analysisRequestId, Guid customerContentId, UpdateDefinition<AnalysisContentNormalizedRequest> update, CancellationToken cancellationToken)
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

    private async Task RecalculateAndUpdateAnalysisParentAsync(Guid analysisRequestId, string currentStep, string? lastError, CancellationToken cancellationToken)
    {
        var analysis = await context.AnalysisRequests
            .Find(x => x.Id == analysisRequestId)
            .FirstAsync(cancellationToken);

        string status = CalculateAnalysisParentStatus(analysis);

        var filter = status == StatusNames.OutlineCompleted
            ? Builders<AnalysisContentNormalizedRequest>.Filter.And(
                Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
                Builders<AnalysisContentNormalizedRequest>.Filter.Ne(x => x.Status, StatusNames.Completed))
            : Builders<AnalysisContentNormalizedRequest>.Filter.And(
                Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
                Builders<AnalysisContentNormalizedRequest>.Filter.Nin(
                    x => x.Status,
                    new[] { StatusNames.Completed, StatusNames.OutlineCompleted }));

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