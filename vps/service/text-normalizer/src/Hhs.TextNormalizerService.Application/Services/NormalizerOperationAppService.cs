using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerOperationAppService(
    IServiceProvider provider,
    ICustomerContentNormalizedRequestRepository customerContentRepository,
    IAnalysisContentNormalizedRequestRepository analysisContentRepository,
    IContentScraper scraper,
    ILogger<NormalizerOperationAppService> logger,
    IOutlineProviderResolver outlineProviderResolver,
    OutlinePollingSettings outlinePollingSettings,
    RetryDelayCalculator retryDelayCalculator,
    NormalizerRetrySettings serviceRetrySettings) : ApplicationServiceBase(provider)
{
    public async Task CreateCustomerContentNormalizeRequestAsync(CustomerContentCreatedEto @event, Guid eventId, [CanBeNull] string correlationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"CreateCustomerContentNormalizeRequestAsync started for RefContentId: {@event.CustomerContentId}, EventId: {eventId}");

        var existing = await customerContentRepository.GetByScopeKeyAndContentIdAsync(@event.ScopeKey, @event.CustomerContentId, cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation($"Existing request found, publishing event");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto { CustomerContentId = existing.CustomerContentId, NormalizeRequestId = existing.Id }
            );

            return;
        }

        logger.LogInformation($"Creating new request...");

        var requestId = Guid.NewGuid();

        try
        {
            logger.LogInformation($"Inserting record into MongoDB...");
            var entity = new CustomerContentNormalizedRequest(
                requestId,
                @event.ScopeKey,
                @event.CustomerContentId,
                @event.DomainName,
                @event.ContentKey,
                correlationId);
            entity.Status = StatusNames.Created;
            entity.CurrentStep = EventNames.CustomerContentCreated;

            await customerContentRepository.InsertAsync(entity, cancellationToken);

            logger.LogInformation($"MongoDB insert successful, publishing event...");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto { CustomerContentId = @event.CustomerContentId, NormalizeRequestId = requestId }
            );

            logger.LogInformation($"Event published successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateCustomerContentNormalizeRequestAsync: {ex.Message}");
            throw;
        }
    }

    public async Task StartCustomerContentNormalizeAsync(CustomerContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetFirstOrDefaultAsync(x => x.CustomerContentId == @event.CustomerContentId, cancellationToken: cancellationToken);

        try
        {
            request.Status = StatusNames.Scraping;
            request.ScrapingStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentScrapingStarted;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

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

    public async Task StartCustomerContentScrapingAsync(CustomerContentScrapingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetFirstOrDefaultAsync(x => x.CustomerContentId == @event.CustomerContentId, cancellationToken: cancellationToken);

        try
        {
            var result = await scraper.ScrapeAsync((request.DomainName + request.ContentKey));

            request.Status = StatusNames.ScrapingCompleted;
            request.ScrapingStatus = StatusNames.Completed;
            request.CurrentStep = EventNames.CustomerContentScrapingCompleted;
            request.ScrapingResult = new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc };
            request.LastError = null;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

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

    public async Task CompleteCustomerContentScrapingAsync(CustomerContentScrapingCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetFirstOrDefaultAsync(x => x.CustomerContentId == @event.CustomerContentId, cancellationToken: cancellationToken);

        try
        {
            request.OutlineStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentOutlineStarted;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

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

    public async Task StartCustomerContentOutlineAsync(CustomerContentOutlineStartedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetFirstOrDefaultAsync(x => x.CustomerContentId == @event.CustomerContentId, cancellationToken: cancellationToken);

        try
        {
            request.Status = StatusNames.OutlineProviderRequestStarted;
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;

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


    public async Task CreateAnalysisContentNormalizeRequestAsync(AnalysisContentCreatedEto @event, Guid eventId, [CanBeNull] string correlationId, CancellationToken cancellationToken = default)
    {
        var existing = await analysisContentRepository.GetFirstOrDefaultAsync(
            x => x.SourceEventId == eventId || x.AnalysisContentId == @event.AnalysisContentId,
            cancellationToken: cancellationToken);

        if (existing is not null)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = existing.AnalysisContentId, NormalizeRequestId = existing.Id }
            );

            return;
        }

        var requestId = Guid.NewGuid();
        var request = new AnalysisContentNormalizedRequest(
            requestId,
            @event.ScopeKey,
            @event.AnalysisContentId,
            @event.DomainName,
            correlationId);

        request.SourceEventId = eventId;
        request.Status = StatusNames.Created;
        request.CurrentStep = EventNames.AnalysisContentCreated;
        request.Items = @event.Items.Select(x => new AnalysisNormalizedItem { CustomerContentId = x.CustomerContentId, SortOrder = x.SortOrder, ContentKey = x.ContentKey }).ToList();

        await analysisContentRepository.InsertAsync(request, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = @event.AnalysisContentId, NormalizeRequestId = requestId }
        );
    }

    public async Task StartAnalysisContentNormalizeAsync(AnalysisContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await analysisContentRepository.GetFirstOrDefaultAsync(x => x.AnalysisContentId == @event.AnalysisContentId, cancellationToken: cancellationToken);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            if (item.ScrapingStatus == StatusNames.Completed)
                continue;

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AnalysisItemScrapingStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
            );
        }
    }

    public async Task StartAnalysisItemScrapingAsync(AnalysisItemScrapingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        try
        {
            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Scraping)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Started)
                    .Set(x => x.Status, StatusNames.Scraping)
                    .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingStarted),
                cancellationToken
            );

            var result = await scraper.ScrapeAsync((request.DomainName + item.ContentKey));

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.ScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Completed)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingResult)}", new ScrapingResult { Title = result.Title, Text = result.Text, ReleaseTimeUtc = result.ReleaseTimeUtc })
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
                    .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingCompleted),
                cancellationToken
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

    public async Task CompleteAnalysisItemScrapingAsync(AnalysisItemScrapingCompletedEto @event, CancellationToken cancellationToken = default)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisItemOutlineStartedEto { AnalysisContentId = @event.AnalysisContentId, CustomerContentIdForItem = @event.CustomerContentIdForItem }
        );
    }

    public async Task StartAnalysisItemOutlineAsync(AnalysisItemOutlineStartedEto @event, CancellationToken cancellationToken = default)
    {
        var analysisContentNormalizedRequest = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        try
        {
            if (item.ScrapingStatus != StatusNames.Completed || item.ScrapingResult is null)
            {
                await UpdateAnalysisItemAsync(
                    analysisContentNormalizedRequest.Id,
                    item.CustomerContentId,
                    u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.WaitingScraping)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", "ScrapingResult is required before outline.")
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.ErrorRescheduleDelaySeconds))
                        .Set(x => x.Status, StatusNames.WaitingRetry)
                        .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineStarted)
                        .Set(x => x.LastError, "ScrapingResult is required before outline."),
                    cancellationToken
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(SubscriptionScopeRegistry.GetOutlineProviderKey(analysisContentNormalizedRequest.ScopeKey)))
                throw new InvalidOperationException("OutlineProviderKey is required.");

            await UpdateAnalysisItemAsync(
                analysisContentNormalizedRequest.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderRequestStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Started)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                    .Set(x => x.Status, StatusNames.OutlineProviderRequestStarted)
                    .Set(x => x.CurrentStep, EventNames.OutlineProviderRequestStarted),
                cancellationToken
            );

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


    public async Task StartOutlineProviderRequestAsync(OutlineProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
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
            var request = await customerContentRepository.GetByIdAsync(@event.NormalizedRequestId);

            request.OutlineProviderTrackId = providerTrackId;
            request.Status = StatusNames.OutlineProviderPolling;
            request.OutlineStatus = StatusNames.Polling;
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;
            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds);

            await customerContentRepository.UpdateAsync(request);
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        await UpdateAnalysisItemAsync(
            @event.NormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderPolling)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineProviderTrackId)}", providerTrackId)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Polling)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds))
                .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted),
            CancellationToken.None
        );
    }

    private async Task HandleOutlineProviderRequestExceptionAsync(OutlineProviderRequestStartedEto @event, Exception ex)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var customerContentNormalizedRequest = await customerContentRepository.GetByIdAsync(@event.NormalizedRequestId);

            await HandleCustomerExceptionAsync(
                customerContentNormalizedRequest,
                EventNames.OutlineProviderRequestStarted,
                ex
            );

            return;
        }

        var analysis = await analysisContentRepository.GetByIdAsync(@event.NormalizedRequestId);

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

    public async Task CompleteOutlineProviderAsync(OutlineProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var customerContentNormalizedRequest = await customerContentRepository.GetByIdAsync(@event.NormalizedRequestId);

            if (customerContentNormalizedRequest.Status == StatusNames.Completed ||
                customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished ||
                customerContentNormalizedRequest.OutlineStatus == StatusNames.Completed)
                return;

            customerContentNormalizedRequest.Status = StatusNames.OutlineCompleted;
            customerContentNormalizedRequest.OutlineStatus = StatusNames.Completed;
            customerContentNormalizedRequest.CurrentStep = EventNames.CustomerContentOutlineCompleted;
            customerContentNormalizedRequest.OutlineResult = new OutlineResult { Script = @event.Script };

            await customerContentRepository.UpdateAsync(customerContentNormalizedRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentOutlineCompletedEto { RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, Script = @event.Script }
            );
            return;
        }

        var analysisContentNormalizedRequest = await analysisContentRepository.GetByIdAsync(@event.NormalizedRequestId);

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline completion.");

        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (item.Status == StatusNames.OutlineCompleted || item.OutlineStatus == StatusNames.Completed)
            return;

        await UpdateAnalysisItemAsync(
            analysisContentNormalizedRequest.Id,
            item.CustomerContentId,
            u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Completed)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", new OutlineResult { Script = @event.Script })
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
                .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineCompleted),
            CancellationToken.None
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


    public async Task CompleteCustomerContentOutlineAsync(CustomerContentOutlineCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var customerContentNormalizedRequest = await customerContentRepository.GetFirstOrDefaultAsync(
            x => x.CustomerContentId == @event.RefContentId,
            cancellationToken: cancellationToken);

        var videoInput = new
        {
            type = "customer",
            customerContentId = customerContentNormalizedRequest.CustomerContentId,
            title = customerContentNormalizedRequest.ScrapingResult?.Title,
            script = customerContentNormalizedRequest.OutlineResult?.Script,
            audioItems = new[] { new { customerContentId = customerContentNormalizedRequest.CustomerContentId, sortOrder = 1, text = customerContentNormalizedRequest.OutlineResult?.Script } }
        };

        if (customerContentNormalizedRequest.Status == StatusNames.Completed ||
            customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished)
            return;

        customerContentNormalizedRequest.Status = StatusNames.Completed;
        customerContentNormalizedRequest.CurrentStep = EventNames.NormalizerResultPublished;

        await customerContentRepository.UpdateAsync(customerContentNormalizedRequest, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizerResultPublishedEto
            {
                NormalizeRequestId = customerContentNormalizedRequest.Id, RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput),
            }
        );
    }

    public async Task CompleteAnalysisItemOutlineAsync(AnalysisItemOutlineCompletedEto @event, CancellationToken cancellationToken = default)
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

        if (analysisContentNormalizedRequest.Status == StatusNames.Completed ||
            analysisContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished)
            return;

        analysisContentNormalizedRequest.Status = StatusNames.Completed;
        analysisContentNormalizedRequest.CurrentStep = EventNames.NormalizerResultPublished;

        await analysisContentRepository.UpdateAsync(analysisContentNormalizedRequest, cancellationToken);

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


    private Task<AnalysisContentNormalizedRequest> GetAnalysisContentNormalizedRequestAsync(Guid analysisContentId)
    {
        return analysisContentRepository.GetFirstOrDefaultAsync(x => x.AnalysisContentId == analysisContentId);
    }

    private async Task FailCustomerAsync(CustomerContentNormalizedRequest request, string step, Exception ex, bool retryable)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        await customerContentRepository.UpdateAsync(request);

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

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailCustomerAsync(request, step, ex, false);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await customerContentRepository.UpdateAsync(request);

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

        if (retryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailAnalysisItemAsync(request, item, step, ex, false);
            return;
        }

        var nextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(retryCount));

        var updateFunc = new Func<UpdateDefinitionBuilder<AnalysisContentNormalizedRequest>, UpdateDefinition<AnalysisContentNormalizedRequest>>(u =>
        {
            var baseUpdate = u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.RetryCount)}", retryCount)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", nextRetryAtUtc)
                .Set(x => x.Status, StatusNames.WaitingRetry)
                .Set(x => x.CurrentStep, step)
                .Set(x => x.LastError, ex.Message);

            if (step == EventNames.AnalysisItemScrapingStarted)
            {
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.WaitingRetry);
            }

            if (step is EventNames.AnalysisItemOutlineStarted
                or EventNames.OutlineProviderRequestStarted
                or EventNames.OutlineProviderPollingStarted)
            {
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.WaitingRetry);
            }

            return baseUpdate;
        });

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            updateFunc,
            CancellationToken.None
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
        var updateFunc = new Func<UpdateDefinitionBuilder<AnalysisContentNormalizedRequest>, UpdateDefinition<AnalysisContentNormalizedRequest>>(u =>
        {
            var baseUpdate = u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Failed)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
                .Set(x => x.Status, StatusNames.Failed)
                .Set(x => x.CurrentStep, step)
                .Set(x => x.LastError, ex.Message);

            if (step == EventNames.AnalysisItemScrapingStarted)
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Failed);

            if (step is EventNames.AnalysisItemOutlineStarted
                or EventNames.OutlineProviderRequestStarted
                or EventNames.OutlineProviderPollingStarted)
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Failed);

            return baseUpdate;
        });

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            updateFunc,
            CancellationToken.None
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

    private async Task UpdateAnalysisParentAsync(Guid id, string status, string currentStep, [CanBeNull] string lastError)
    {
        var request = await analysisContentRepository.GetByIdAsync(id);
        request.Status = status;
        request.CurrentStep = currentStep;
        request.LastError = lastError;

        await analysisContentRepository.UpdateAsync(request);
    }

    private Task<long> UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        Func<UpdateDefinitionBuilder<AnalysisContentNormalizedRequest>, UpdateDefinition<AnalysisContentNormalizedRequest>> updateFunc,
        CancellationToken cancellationToken = default)
    {
        var predicate = (Expression<Func<AnalysisContentNormalizedRequest, bool>>)(
            x => x.Id == analysisRequestId &&
                 x.Items.Any(i => i.CustomerContentId == customerContentId));

        return analysisContentRepository.UpdateByExpressionAsync(predicate, updateFunc, cancellationToken);
    }

    private async Task RecalculateAndUpdateAnalysisParentAsync(Guid analysisRequestId, string currentStep, [CanBeNull] string lastError)
    {
        var analysis = await analysisContentRepository.GetByIdAsync(analysisRequestId);

        string status = CalculateAnalysisParentStatus(analysis);

        // Only update if status is not already completed
        if (status == StatusNames.OutlineCompleted && analysis.Status == StatusNames.Completed)
            return;

        if (status != StatusNames.OutlineCompleted &&
            (analysis.Status == StatusNames.Completed || analysis.Status == StatusNames.OutlineCompleted))
            return;

        analysis.Status = status;
        analysis.CurrentStep = currentStep;
        analysis.LastError = lastError;

        await analysisContentRepository.UpdateAsync(analysis);
    }
}