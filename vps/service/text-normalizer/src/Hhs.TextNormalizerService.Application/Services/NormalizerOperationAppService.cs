using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerOperationAppService(
    IServiceProvider provider,
    TextNormalizerServiceDbContext context,
    IContentScraper scraper,
    ILogger<NormalizerOperationAppService> logger,
    IOutlineProviderResolver outlineProviderResolver,
    OutlinePollingSettings outlinePollingSettings,
    RetryDelayCalculator retryDelayCalculator) : ApplicationServiceBase(provider)
{
    public async Task CreateCustomerContentNormalizeRequestAsync(CustomerContentCreatedEto @event, Guid eventId, [CanBeNull] string correlationId)
    {
        logger.LogInformation($"CreateCustomerContentNormalizeRequestAsync started for RefContentId: {@event.CustomerContentId}, EventId: {eventId}");

        var existing = await context.CustomerContentNormalizedRequests
            .Find(x => x.SourceEventId == eventId || x.CustomerContentId == @event.CustomerContentId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            logger.LogInformation($"Existing request found, publishing event");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto { CustomerContentId = existing.CustomerContentId }
            );

            return;
        }

        logger.LogInformation($"Creating new request...");

        var requestId = Guid.NewGuid();

        try
        {
            logger.LogInformation($"Inserting record into MongoDB...");
            await context.CustomerContentNormalizedRequests.InsertOneAsync(new CustomerContentNormalizedRequest
            {
                Id = requestId,
                SourceEventId = eventId,
                CorrelationId = correlationId,
                ScopeKey = @event.ScopeKey,
                CustomerContentId = @event.CustomerContentId,
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
                MaxOutlinePollingCount = outlinePollingSettings.MaxAttempts,
                // event retry mechanism
                RetryCount = 0,
                NextRetryAtUtc = null,
                // audit
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            });

            logger.LogInformation($"MongoDB insert successful, publishing event...");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto { CustomerContentId = @event.CustomerContentId, }
            );

            logger.LogInformation($"Event published successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateCustomerContentNormalizeRequestAsync: {ex.Message}");
            throw;
        }
    }

    public async Task StartCustomerContentNormalizeAsync(CustomerContentNormalizeRequestCreatedEto @event)
    {
        var request = await context.CustomerContentNormalizedRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync();

        try
        {
            request.Status = StatusNames.Scraping;
            request.ScrapingStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentScrapingStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentScrapingStartedEto { CustomerContentId = @event.CustomerContentId }
            );
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerContentScrapingStarted,
                ex
            );
        }
    }

    public async Task StartCustomerContentScrapingAsync(CustomerContentScrapingStartedEto @event)
    {
        var request = await context.CustomerContentNormalizedRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync();

        try
        {
            var result = await scraper.ScrapeAsync((request.DomainName + request.ContentKey));

            request.Status = StatusNames.ScrapingCompleted;
            request.ScrapingStatus = StatusNames.Completed;
            request.CurrentStep = EventNames.CustomerContentScrapingCompleted;
            request.ScrapingResult = new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc };
            request.LastError = null;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentScrapingCompletedEto { CustomerContentId = request.CustomerContentId }
            );
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerContentScrapingCompleted,
                ex
            );
        }
    }

    public async Task CompleteCustomerContentScrapingAsync(CustomerContentScrapingCompletedEto @event)
    {
        var request = await context.CustomerContentNormalizedRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync();

        try
        {
            request.OutlineStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentOutlineStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentOutlineStartedEto { CustomerContentId = @event.CustomerContentId }
            );
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.CustomerContentOutlineStarted,
                ex
            );
        }
    }

    public async Task StartCustomerContentOutlineAsync(CustomerContentOutlineStartedEto @event)
    {
        var request = await context.CustomerContentNormalizedRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync();

        try
        {
            request.Status = StatusNames.OutlineProviderRequestStarted;
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            if (request.ScrapingResult is null)
                throw new InvalidOperationException("ScrapingResult is required before outline.");

            logger.LogInformation($"Publishing OutlineProviderRequestStartedEto with ScopeKey='{request.ScopeKey}' (null={request.ScopeKey == null})");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new OutlineProviderRequestStartedEto
                {
                    CustomerContentIdForItem = request.CustomerContentId,
                    RefContentType = ContentType.CustomerContent,
                    NormalizedRequestId = request.Id,
                    ScopeKey = request.ScopeKey,
                    InputText = request.ScrapingResult.Text
                }
            );
        }
        catch (Exception ex)
        {
            await HandleCustomerExceptionAsync(
                request,
                EventNames.OutlineProviderRequestStarted,
                ex
            );
        }
    }


    public async Task CreateAnalysisContentNormalizeRequestAsync(AnalysisContentCreatedEto @event, Guid eventId, [CanBeNull] string correlationId)
    {
        var existing = await context.AnalysisContentNormalizedRequests
            .Find(x => x.SourceEventId == eventId || x.AnalysisContentId == @event.AnalysisContentId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = existing.AnalysisContentId }
            );

            return;
        }

        var request = new AnalysisContentNormalizedRequest
        {
            Id = Guid.NewGuid(),
            SourceEventId = eventId,
            CorrelationId = correlationId,
            ScopeKey = @event.ScopeKey,
            DomainName = @event.DomainName,
            AnalysisContentId = @event.AnalysisContentId,
            Status = StatusNames.Created,
            CurrentStep = EventNames.AnalysisContentCreated,
            Items = @event.Items.Select(x => new AnalysisNormalizedItem { CustomerContentId = x.CustomerContentId, SortOrder = x.SortOrder, ContentKey = x.ContentKey }).ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await context.AnalysisContentNormalizedRequests.InsertOneAsync(request);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = @event.AnalysisContentId }
        );
    }

    public async Task StartAnalysisContentNormalizeAsync(AnalysisContentNormalizeRequestCreatedEto @event)
    {
        var request = await context.AnalysisContentNormalizedRequests
            .Find(x => x.AnalysisContentId == @event.AnalysisContentId)
            .FirstAsync();

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            if (item.ScrapingStatus == StatusNames.Completed)
                continue;

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AnalysisItemScrapingStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
            );
        }
    }

    public async Task StartAnalysisItemScrapingAsync(AnalysisItemScrapingStartedEto @event)
    {
        var request = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

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
                startUpdate
            );

            var result = await scraper.ScrapeAsync((request.DomainName + item.ContentKey));

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
                completeUpdate
            );

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AnalysisItemScrapingCompletedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = @event.CustomerContentIdForItem, }
            );
        }
        catch (Exception ex)
        {
            await HandleAnalysisItemExceptionAsync(
                request,
                item,
                EventNames.AnalysisItemScrapingStarted,
                ex
            );
        }
    }

    public async Task CompleteAnalysisItemScrapingAsync(AnalysisItemScrapingCompletedEto @event)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisItemOutlineStartedEto { AnalysisContentId = @event.AnalysisContentId, CustomerContentIdForItem = @event.CustomerContentIdForItem }
        );
    }

    public async Task StartAnalysisItemOutlineAsync(AnalysisItemOutlineStartedEto @event)
    {
        var analysisContentNormalizedRequest = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        try
        {
            if (item.ScrapingStatus != StatusNames.Completed || item.ScrapingResult is null)
            {
                var update = Builders<AnalysisContentNormalizedRequest>.Update
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.WaitingScraping)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", "ScrapingResult is required before outline.")
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.ErrorRescheduleDelaySeconds))
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                    .Set(x => x.Status, StatusNames.WaitingRetry)
                    .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineStarted)
                    .Set(x => x.LastError, "ScrapingResult is required before outline.")
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                await UpdateAnalysisItemAsync(analysisContentNormalizedRequest.Id, item.CustomerContentId, update);
                return;
            }

            if (string.IsNullOrWhiteSpace(SubscriptionScopeRegistry.GetOutlineProviderKey(analysisContentNormalizedRequest.ScopeKey)))
                throw new InvalidOperationException("OutlineProviderKey is required.");

            var startUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderRequestStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Started)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                .Set(x => x.Status, StatusNames.OutlineProviderRequestStarted)
                .Set(x => x.CurrentStep, EventNames.OutlineProviderRequestStarted)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await UpdateAnalysisItemAsync(analysisContentNormalizedRequest.Id, item.CustomerContentId, startUpdate);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new OutlineProviderRequestStartedEto
                {
                    CustomerContentIdForItem = item.CustomerContentId,
                    RefContentType = ContentType.AnalysisContent,
                    NormalizedRequestId = analysisContentNormalizedRequest.Id,
                    ScopeKey = analysisContentNormalizedRequest.ScopeKey,
                    InputText = item.ScrapingResult.Text
                }
            );
        }
        catch (Exception ex)
        {
            await HandleAnalysisItemExceptionAsync(
                analysisContentNormalizedRequest,
                item,
                EventNames.AnalysisItemOutlineStarted,
                ex
            );
        }
    }


    public async Task StartOutlineProviderRequestAsync(OutlineProviderRequestStartedEto @event)
    {
        logger.LogInformation($"StartOutlineProviderRequestAsync - Event ScopeKey: '{@event.ScopeKey}' (null={@event.ScopeKey == null}, empty={string.IsNullOrWhiteSpace(@event.ScopeKey)})");

        try
        {
            var providerKey = SubscriptionScopeRegistry.GetOutlineProviderKey(@event.ScopeKey);
            logger.LogInformation($"Got providerKey from registry: '{providerKey}' (null={providerKey == null})");

            var provider = outlineProviderResolver.Resolve(providerKey);
            logger.LogInformation($"✓ Resolved provider successfully");

            var response = await provider.CreateAsync(new OutlineCreateRequest { OutlineInput = @event.InputText });

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.OutlinedData))
                    throw new InvalidOperationException("Script is required for immediate outline provider.");

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new OutlineProviderCompletedEto { RefContentType = @event.RefContentType, NormalizedRequestId = @event.NormalizedRequestId, CustomerContentIdForItem = @event.CustomerContentIdForItem, Script = response.OutlinedData }
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("ProviderTrackId is required for async outline provider.");

            await SaveOutlinePollingStateAsync(@event, response.ProviderTrackId);
        }
        catch (Exception ex)
        {
            await HandleOutlineProviderRequestExceptionAsync(@event, ex);
        }
    }

    private async Task SaveOutlinePollingStateAsync(OutlineProviderRequestStartedEto @event, string providerTrackId)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var request = await context.CustomerContentNormalizedRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync();

            request.OutlineProviderTrackId = providerTrackId;
            request.Status = StatusNames.OutlineProviderPolling;
            request.OutlineStatus = StatusNames.Polling;
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;
            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds);
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request);
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderPolling)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineProviderTrackId)}", providerTrackId)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Polling)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds))
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
            .Set(x => x.Status, StatusNames.OutlineProviderPolling)
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            @event.NormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            update
        );
    }

    private async Task HandleOutlineProviderRequestExceptionAsync(OutlineProviderRequestStartedEto @event, Exception ex)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var customerContentNormalizedRequest = await context.CustomerContentNormalizedRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync();

            await HandleCustomerExceptionAsync(
                customerContentNormalizedRequest,
                EventNames.OutlineProviderRequestStarted,
                ex
            );

            return;
        }

        var analysis = await context.AnalysisContentNormalizedRequests
            .Find(x => x.Id == @event.NormalizedRequestId)
            .FirstAsync();

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required.");

        var item = analysis.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        await HandleAnalysisItemExceptionAsync(
            analysis,
            item,
            EventNames.OutlineProviderRequestStarted,
            ex
        );
    }

    public async Task CompleteOutlineProviderAsync(OutlineProviderCompletedEto @event)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var customerContentNormalizedRequest = await context.CustomerContentNormalizedRequests
                .Find(x => x.Id == @event.NormalizedRequestId)
                .FirstAsync();

            if (customerContentNormalizedRequest.Status == StatusNames.Completed ||
                customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished ||
                customerContentNormalizedRequest.OutlineStatus == StatusNames.Completed)
                return;

            customerContentNormalizedRequest.Status = StatusNames.OutlineCompleted;
            customerContentNormalizedRequest.OutlineStatus = StatusNames.Completed;
            customerContentNormalizedRequest.CurrentStep = EventNames.CustomerContentOutlineCompleted;
            customerContentNormalizedRequest.OutlineResult = new OutlineResult { Script = @event.Script };
            customerContentNormalizedRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(customerContentNormalizedRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentOutlineCompletedEto { RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, Script = @event.Script }
            );
            return;
        }

        var analysisContentNormalizedRequest = await context.AnalysisContentNormalizedRequests
            .Find(x => x.Id == @event.NormalizedRequestId)
            .FirstAsync();

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline completion.");

        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

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
            analysisContentNormalizedRequest.Id,
            item.CustomerContentId,
            update
        );

        await RecalculateAndUpdateAnalysisParentAsync(
            analysisContentNormalizedRequest.Id,
            EventNames.AnalysisItemOutlineCompleted,
            null
        );

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisItemOutlineCompletedEto { AnalysisContentId = analysisContentNormalizedRequest.AnalysisContentId }
        );
    }


    public async Task CompleteCustomerContentOutlineAsync(CustomerContentOutlineCompletedEto @event)
    {
        var customerContentNormalizedRequest = await context.CustomerContentNormalizedRequests
            .Find(x => x.CustomerContentId == @event.RefContentId)
            .FirstAsync();

        var videoInput = new
        {
            type = "customer",
            customerContentId = customerContentNormalizedRequest.CustomerContentId,
            title = customerContentNormalizedRequest.ScrapingResult?.Title,
            script = customerContentNormalizedRequest.OutlineResult?.Script,
            audioItems = new[] { new { customerContentId = customerContentNormalizedRequest.CustomerContentId, sortOrder = 1, text = customerContentNormalizedRequest.OutlineResult?.Script } }
        };

        var completeResult = await context.CustomerContentNormalizedRequests.UpdateOneAsync(
            x =>
                x.Id == customerContentNormalizedRequest.Id &&
                x.Status != StatusNames.Completed &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            Builders<CustomerContentNormalizedRequest>.Update
                .Set(x => x.Status, StatusNames.Completed)
                .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
        );

        if (completeResult.ModifiedCount == 0)
            return;

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizerResultPublishedEto
            {
                NormalizeRequestId = customerContentNormalizedRequest.Id, RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
            }
        );
    }

    public async Task CompleteAnalysisItemOutlineAsync(AnalysisItemOutlineCompletedEto @event)
    {
        var analysisContentNormalizedRequest = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);

        if (analysisContentNormalizedRequest is { Status: StatusNames.Completed, CurrentStep: EventNames.NormalizerResultPublished })
        {
            return;
        }

        if (analysisContentNormalizedRequest.Items.Any(x => x.Status == StatusNames.Failed))
        {
            await UpdateAnalysisParentAsync(
                analysisContentNormalizedRequest.Id,
                StatusNames.Failed,
                EventNames.AnalysisItemOutlineCompleted,
                "One or more items failed during processing."
            );
            return;
        }

        if (analysisContentNormalizedRequest.Items.Any(x => x.OutlineStatus != StatusNames.Completed))
            return;

        var videoInput = new
        {
            type = "analysis",
            analysisContentId = analysisContentNormalizedRequest.AnalysisContentId,
            audioItems = analysisContentNormalizedRequest.Items
                .OrderBy(x => x.SortOrder)
                .Select(x => new { customerContentId = x.CustomerContentId, sortOrder = x.SortOrder, title = x.ScrapingResult?.Title, text = x.OutlineResult?.Script })
                .ToList()
        };

        var completeUpdate = Builders<AnalysisContentNormalizedRequest>.Update
            .Set(x => x.Status, StatusNames.Completed)
            .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        var completeResult = await context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            x =>
                x.Id == analysisContentNormalizedRequest.Id &&
                x.Status != StatusNames.Completed &&
                x.CurrentStep != EventNames.NormalizerResultPublished,
            completeUpdate
        );

        if (completeResult.ModifiedCount == 0)
            return;

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizerResultPublishedEto
            {
                RefContentType = ContentType.AnalysisContent, RefContentId = analysisContentNormalizedRequest.AnalysisContentId, NormalizeRequestId = analysisContentNormalizedRequest.Id, VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
            }
        );
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

    private Task ReplaceCustomerAsync(CustomerContentNormalizedRequest request)
    {
        return context.CustomerContentNormalizedRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request
        );
    }

    private Task<AnalysisContentNormalizedRequest> GetAnalysisContentNormalizedRequestAsync(Guid analysisContentId)
    {
        return context.AnalysisContentNormalizedRequests
            .Find(x => x.AnalysisContentId == analysisContentId)
            .FirstAsync();
    }

    private async Task FailCustomerAsync(CustomerContentNormalizedRequest request, string step, Exception ex, bool retryable)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.CustomerContentId,
                RefContentType = ContentType.CustomerContent,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = retryable
            }
        );
    }

    private async Task HandleCustomerExceptionAsync(CustomerContentNormalizedRequest request, string step, Exception ex)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleCustomerRetryAsync(request, step, ex);
            return;
        }

        await FailCustomerAsync(request, step, ex, false);
    }

    private async Task ScheduleCustomerRetryAsync(CustomerContentNormalizedRequest request, string step, Exception ex)
    {
        request.RetryCount++;

        if (request.RetryCount >= request.MaxRetryCount)
        {
            await FailCustomerAsync(request, step, ex, false);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.CustomerContentId,
                RefContentType = ContentType.CustomerContent,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task HandleAnalysisItemExceptionAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAnalysisItemRetryAsync(request, item, step, ex);
            return;
        }

        await FailAnalysisItemAsync(request, item, step, ex, false);
    }

    private async Task ScheduleAnalysisItemRetryAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex)
    {
        int retryCount = item.RetryCount + 1;

        if (retryCount >= item.MaxRetryCount)
        {
            await FailAnalysisItemAsync(request, item, step, ex, false);
            return;
        }

        var nextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(retryCount));

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
            update
        );

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = item.CustomerContentId,
                RefContentType = ContentType.AnalysisContent,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task FailAnalysisItemAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, string step, Exception ex, bool retryable)
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
            update
        );

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = item.CustomerContentId,
                RefContentType = ContentType.AnalysisContent,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = retryable
            }
        );
    }

    private Task UpdateAnalysisParentAsync(Guid id, string status, string currentStep, string? lastError)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set(x => x.Status, status)
            .Set(x => x.CurrentStep, currentStep)
            .Set(x => x.LastError, lastError)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            x => x.Id == id,
            update
        );
    }

    private Task UpdateAnalysisItemAsync(Guid analysisRequestId, Guid customerContentId, UpdateDefinition<AnalysisContentNormalizedRequest> update)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i => i.CustomerContentId == customerContentId));

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            filter,
            update
        );
    }

    private async Task RecalculateAndUpdateAnalysisParentAsync(Guid analysisRequestId, string currentStep, string? lastError)
    {
        var analysis = await context.AnalysisContentNormalizedRequests
            .Find(x => x.Id == analysisRequestId)
            .FirstAsync();

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

        await context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            filter,
            update
        );
    }
}