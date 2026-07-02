using System.Linq.Expressions;
using System.Net;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Application.Providers.Scraping;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Settings;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerOperationAppService(
    IServiceProvider provider,
    ICustomerContentNormalizedRequestRepository customerContentRepository,
    IAnalysisContentNormalizedRequestRepository analysisContentRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IContentScraper scraper,
    IOutlineProviderResolver outlineProviderResolver,
    OutlinePollingSettings outlinePollingSettings,
    RetryDelayCalculator retryDelayCalculator,
    NormalizerRetrySettings serviceRetrySettings,
    IOptions<TextNormalizerSettings> normalizerSettings) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly TextNormalizerSettings _normalizerSettings = normalizerSettings.Value;

    public async Task CreateCustomerContentNormalizeRequestAsync(CustomerContentCreatedEto @event, Guid eventId, [CanBeNull] string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"CreateCustomerContentNormalizeRequestAsync started for RefContentId: {@event.CustomerContentId}, EventId: {eventId}");

        var existing = await customerContentRepository.GetByScopeKeyAndContentIdAsync(@event.ScopeKey, @event.CustomerContentId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogDebug($"Existing request found, publishing event");

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto
                {
                    CustomerContentId = existing.CustomerContentId, // for content service update
                    CustomerContentNormalizeRequestId = existing.Id
                }
            );

            return;
        }

        _logger.LogDebug($"Creating new request...");

        var requestId = Guid.CreateVersion7();

        try
        {
            var entity = new CustomerContentNormalizedRequest(
                requestId,
                @event.ScopeKey,
                @event.CustomerContentId,
                StringHelper.Minimize(@event.DomainName),
                StringHelper.Minimize(@event.DomainPath),
                correlationId);

            entity.SourceEventId = eventId;
            entity.Status = StatusNames.Created;
            entity.CurrentStep = EventNames.CustomerContentNormalizeRequestCreated;
            entity.ScrapingStatus = StatusNames.NotStarted;
            entity.OutlineStatus = StatusNames.NotStarted;

            await customerContentRepository.InsertAsync(entity, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentNormalizeRequestCreated,
                reference: new
                {
                    ScopeKey = entity.ScopeKey,
                    ClientDomain = entity.DomainName,
                    ContentKey = entity.ContentKey,
                    RefContentId = entity.CustomerContentId,
                    RefNormalizedRequestId = entity.Id
                },
                facility: EventNames.CustomerContentNormalizeRequestCreated,
                correlationId: correlationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto
                {
                    CustomerContentId = @event.CustomerContentId, // for content service update
                    CustomerContentNormalizeRequestId = requestId
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in CreateCustomerContentNormalizeRequestAsync: {ex.Message}");
            throw;
        }
    }

    public async Task StartCustomerContentNormalizeAsync(CustomerContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetByIdOrDefaultAsync(@event.CustomerContentNormalizeRequestId, cancellationToken: cancellationToken);
        if (request == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        try
        {
            request.Status = StatusNames.Scraping;
            request.ScrapingStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentScrapingStarted;

            await customerContentRepository.UpdateAsync(request, cancellationToken);


            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentScrapingStarted,
                reference: new
                {
                    request.ScopeKey,
                    ClientDomain = request.DomainName,
                    ContentKey = request.ContentKey,
                    RefContentId = request.CustomerContentId,
                    RefNormalizedRequestId = request.Id
                },
                facility: EventNames.CustomerContentScrapingStarted,
                correlationId: request.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentScrapingStartedEto { CustomerContentNormalizeRequestId = @event.CustomerContentNormalizeRequestId }
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
        var request = await customerContentRepository.GetByIdOrDefaultAsync(@event.CustomerContentNormalizeRequestId, cancellationToken: cancellationToken);
        if (request == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        try
        {
            if (string.IsNullOrEmpty(request?.DomainName) || string.IsNullOrEmpty(request?.ContentKey))
            {
                throw new BaseHttpException((int)HttpStatusCode.BadRequest);
            }

            ScraperResultDto result;
            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == request.ScopeKey, cancellationToken: cancellationToken);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(request.ScopeKey));

            if (!_normalizerSettings.SkipScrapingOperation && customerVpSetting.IsScrapingOperationActive)
            {
                result = await scraper.ScrapeAsync(new ScraperRequestDto { DomainKey = request.DomainName, Path = request.ContentKey });
            }
            else
            {
                result = await new DummyContentScraper().ScrapeAsync(new ScraperRequestDto { DomainKey = request.DomainName, Path = request.ContentKey });
            }

            if (result == null)
            {
                throw new Exception("SCRAPING DATA NOT FOUND");
            }

            if (result.HasError)
            {
                throw new Exception(string.Join(" ", result.Errors));
            }

            if (!(!string.IsNullOrWhiteSpace(result.Title)
                  || !string.IsNullOrWhiteSpace(result.Spot)
                  || !string.IsNullOrWhiteSpace(result.Details)))
            {
                throw new Exception("SCRAPING DATA IS EMPTY");
            }

            request.Status = StatusNames.ScrapingCompleted;
            request.ScrapingStatus = StatusNames.Completed;
            request.CurrentStep = EventNames.CustomerContentScrapingCompleted;
            request.ScrapingResult = new ScrapingContentDataModel
            {
                Title = result.Title ?? string.Empty,
                Spot = result.Spot,
                ReleaseTimeUtc = result.ReleaseTimeUtc,
                Details = result.Details,
                ImageUrl = result.ImageUrl
            };
            request.LastError = null;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentScrapingCompleted,
                reference: new
                {
                    request.ScopeKey,
                    ClientDomain = request.DomainName,
                    ContentKey = request.ContentKey,
                    RefContentId = request.CustomerContentId,
                    RefNormalizedRequestId = request.Id
                },
                facility: EventNames.CustomerContentScrapingCompleted,
                correlationId: request.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentScrapingCompletedEto
                {
                    // Update reference for Content Service
                    CustomerContentId = request.CustomerContentId, ScrapedReleaseTimeUtc = result.ReleaseTimeUtc, CustomerContentNormalizeRequestId = @event.CustomerContentNormalizeRequestId
                }
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
        var request = await customerContentRepository.GetByIdOrDefaultAsync(@event.CustomerContentNormalizeRequestId, cancellationToken: cancellationToken);
        if (request == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        try
        {
            request.OutlineStatus = StatusNames.Started;
            request.CurrentStep = EventNames.CustomerContentOutlineStarted;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentOutlineStarted,
                reference: new
                {
                    request.ScopeKey,
                    ClientDomain = request.DomainName,
                    ContentKey = request.ContentKey,
                    RefContentId = request.CustomerContentId,
                    RefNormalizedRequestId = request.Id
                },
                facility: EventNames.CustomerContentOutlineStarted,
                correlationId: request.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentOutlineStartedEto { CustomerContentNormalizeRequestId = @event.CustomerContentNormalizeRequestId }
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
        var request = await customerContentRepository.GetByIdOrDefaultAsync(@event.CustomerContentNormalizeRequestId, cancellationToken: cancellationToken);
        if (request == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        try
        {
            if (request.ScrapingResult is null)
                throw new InvalidOperationException("OUTLINE SCRAPING DATA UNKNOWN");

            if (request.ScrapingResult.ReleaseTimeUtc != null
                && request.ScrapingResult.ReleaseTimeUtc < DateTime.UtcNow.AddDays(-3)
                && !_normalizerSettings.UseReleaseTimeOldContentOutlineOperation)
            {
                request.Status = StatusNames.OutlineSkipped;
                request.CurrentStep = EventNames.NormalizerResultPublished;

                await customerContentRepository.UpdateAsync(request, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: "Content Normalized request outline skipped",
                    reference: new
                    {
                        request.ScopeKey,
                        ClientDomain = request.DomainName,
                        ContentKey = request.ContentKey,
                        RefContentId = request.CustomerContentId,
                        RefNormalizedRequestId = request.Id
                    },
                    facility: "CONTENT_NORMALIZED_REQUEST_OUTLINE_SKIPPED",
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new NormalizerResultPublishedEto { RefContentId = request.CustomerContentId, RefContentType = ContentType.CustomerContent, NormalizeRequestId = request.Id, NormalizeStatus = request.Status }
                );

                return;
            }

            request.Status = StatusNames.OutlineProviderRequestStarted;
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;

            await customerContentRepository.UpdateAsync(request, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.OutlineProviderRequestStarted,
                reference: new
                {
                    request.ScopeKey,
                    ClientDomain = request.DomainName,
                    ContentKey = request.ContentKey,
                    RefContentId = request.CustomerContentId,
                    RefNormalizedRequestId = request.Id
                },
                facility: EventNames.OutlineProviderRequestStarted,
                correlationId: request.CorrelationId,
                exception: null
            ));

            string outlineInputPrompt = await customerVpSettingRepository.GetContentOutlinePromptByScopeKeyAsync(request.ScopeKey, cancellationToken);

            string outlineInputText = string.Format("{0} {1} {2}",
                request.ScrapingResult.Title,
                request.ScrapingResult.Spot ?? string.Empty,
                request.ScrapingResult.Details ?? string.Empty
            );

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new OutlineProviderRequestStartedEto
                {
                    RefNormalizedRequestId = request.Id,
                    CustomerContentIdForItem = request.CustomerContentId,
                    RefContentType = ContentType.CustomerContent,
                    ScopeKey = request.ScopeKey,
                    InputText = outlineInputText,
                    InputPrompt = outlineInputPrompt,
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
                eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = existing.AnalysisContentId, AnalysisContentNormalizeRequestId = existing.Id }
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
            eventMessage: new AnalysisContentNormalizeRequestCreatedEto { AnalysisContentId = @event.AnalysisContentId, AnalysisContentNormalizeRequestId = requestId }
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
        if (request.Status == StatusNames.Failed) return;

        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (!_normalizerSettings.ForceReScrapeForAnalysis)
        {
            var existingRequest = await customerContentRepository.GetByScopeKeyAndContentIdAsync(request.ScopeKey, item.CustomerContentId, cancellationToken);
            if (existingRequest?.ScrapingStatus == StatusNames.Completed && existingRequest.ScrapingResult is not null)
            {
                await UpdateAnalysisItemAsync(
                    request.Id,
                    item.CustomerContentId,
                    u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.ScrapingCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Completed)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingResult)}", existingRequest.ScrapingResult)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", default(string))
                        .Set(x => x.CurrentStep, EventNames.AnalysisItemScrapingCompleted),
                    cancellationToken
                );
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new AnalysisItemScrapingCompletedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = @event.CustomerContentIdForItem }
                );
                return;
            }
        }

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

            var result = await scraper.ScrapeAsync(new ScraperRequestDto { DomainKey = request.DomainName, Path = item.ContentKey });

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.ScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", StatusNames.Completed)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingResult)}",
                        new ScrapingContentDataModel
                        {
                            Title = result.Title ?? string.Empty,
                            Spot = result.Spot,
                            ReleaseTimeUtc = result.ReleaseTimeUtc,
                            Details = result.Details,
                            ImageUrl = result.ImageUrl
                        }
                    )
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
        if (analysisContentNormalizedRequest.Status == StatusNames.Failed) return;

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

            CustomerVpSetting analysisVpSetting = await customerVpSettingRepository.GetFirstOrDefaultAsync(
                x => x.ScopeKey == analysisContentNormalizedRequest.ScopeKey,
                cancellationToken: cancellationToken);

            string outlineInputText = analysisVpSetting?.IsForceContentDetailInAnalyseActive == true
                ? item.ScrapingResult.Details
                : item.ScrapingResult.Title.Replace(":", "") + " : " + (item.ScrapingResult.Spot ?? item.ScrapingResult.Details);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new OutlineProviderRequestStartedEto
                {
                    CustomerContentIdForItem = item.CustomerContentId,
                    RefContentType = ContentType.AnalysisContent,
                    RefNormalizedRequestId = analysisContentNormalizedRequest.Id,
                    ScopeKey = analysisContentNormalizedRequest.ScopeKey,
                    InputText = outlineInputText,
                    InputPrompt = analysisVpSetting?.AnalysisOutlineContentPrompt
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
        _logger.LogInformation($"StartOutlineProviderRequestAsync - Event ScopeKey: '{@event.ScopeKey}' (null={@event.ScopeKey == null}, empty={string.IsNullOrWhiteSpace(@event.ScopeKey)})");

        try
        {
            var providerKeyResult = await customerVpSettingRepository.GetOutlineProviderKeyByScopeKeyAsync(@event.ScopeKey, cancellationToken);
            if (!providerKeyResult.Key)
            {
                throw new InvalidOperationException($"Provider key value is unknown. Scope key: {@event.ScopeKey}");
            }

            var outlineProvider = outlineProviderResolver.Resolve(providerKeyResult.Value);
            _logger.LogInformation($"✓ Resolved provider successfully");

            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == @event.ScopeKey, cancellationToken: cancellationToken);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(@event.ScopeKey));

            OutlineCreateResponse response;
            if (!_normalizerSettings.SkipOutlineOperation && customerVpSetting.IsOutlineOperationActive)
            {
                response = await outlineProvider.OutlineOperationAsync(new OutlineCreateRequest { OutlineInput = @event.InputText, OutlinePrompt = @event.InputPrompt, UseStructuredOutput = _normalizerSettings.UseStructuredOutput, });
            }
            else
            {
                // dummy response
                response = new OutlineCreateResponse { IsProcessed = true, OutlinedData = @event.InputText };
            }

            if (response.IsProcessFailed)
            {
                throw new InvalidOperationException("OUTLINE_PROVIDER_PROCESS_FAILED: " + response.ErrorMessage);
            }

            if (outlineProvider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult
                || _normalizerSettings.SkipOutlineOperation
                || !customerVpSetting.IsOutlineOperationActive)
            {
                if (string.IsNullOrWhiteSpace(response.OutlinedData))
                    throw new InvalidOperationException("OUTLINE RESPONSE CONTENT DATA UNKNOWN");

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new OutlineProviderCompletedEto { RefContentType = @event.RefContentType, RefNormalizedRequestId = @event.RefNormalizedRequestId, CustomerContentIdForItem = @event.CustomerContentIdForItem, OutlinedData = response.OutlinedData }
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
            var request = await customerContentRepository.GetByIdAsync(@event.RefNormalizedRequestId);

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
            @event.RefNormalizedRequestId,
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
            var customerContentNormalizedRequest = await customerContentRepository.GetByIdAsync(@event.RefNormalizedRequestId);

            await HandleCustomerExceptionAsync(
                customerContentNormalizedRequest,
                EventNames.OutlineProviderRequestStarted,
                ex
            );

            return;
        }

        var analysis = await analysisContentRepository.GetByIdAsync(@event.RefNormalizedRequestId);

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
            var customerContentNormalizedRequest = await customerContentRepository.GetByIdAsync(@event.RefNormalizedRequestId, cancellationToken: cancellationToken);

            if (customerContentNormalizedRequest.Status == StatusNames.Completed ||
                customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished ||
                customerContentNormalizedRequest.OutlineStatus == StatusNames.Completed)
                return;

            customerContentNormalizedRequest.Status = StatusNames.OutlineCompleted;
            customerContentNormalizedRequest.OutlineStatus = StatusNames.Completed;
            customerContentNormalizedRequest.CurrentStep = EventNames.CustomerContentOutlineCompleted;
            customerContentNormalizedRequest.OutlineResult = new OutlineResult { OutlinedData = @event.OutlinedData };

            await customerContentRepository.UpdateAsync(customerContentNormalizedRequest, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentOutlineCompletedEto { RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, Script = @event.OutlinedData }
            );
            return;
        }

        var analysisContentNormalizedRequest = await analysisContentRepository.GetByIdAsync(@event.RefNormalizedRequestId, cancellationToken: cancellationToken);

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
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", new OutlineResult { OutlinedData = @event.OutlinedData })
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

        if (customerContentNormalizedRequest.Status == StatusNames.Completed ||
            customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished)
            return;

        customerContentNormalizedRequest.Status = StatusNames.Completed;
        customerContentNormalizedRequest.CurrentStep = EventNames.NormalizerResultPublished;

        await customerContentRepository.UpdateAsync(customerContentNormalizedRequest, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizerResultPublishedEto { RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, NormalizeRequestId = customerContentNormalizedRequest.Id, NormalizeStatus = customerContentNormalizedRequest.Status }
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

        if (analysisContentNormalizedRequest.Status == StatusNames.Completed ||
            analysisContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished)
            return;

        await AppendIntroOutroToAnalysisItemsAsync(analysisContentNormalizedRequest, cancellationToken);

        analysisContentNormalizedRequest.Status = StatusNames.Completed;
        analysisContentNormalizedRequest.CurrentStep = EventNames.NormalizerResultPublished;

        await analysisContentRepository.UpdateAsync(analysisContentNormalizedRequest, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizerResultPublishedEto { RefContentType = ContentType.AnalysisContent, RefContentId = analysisContentNormalizedRequest.AnalysisContentId, NormalizeRequestId = analysisContentNormalizedRequest.Id, NormalizeStatus = analysisContentNormalizedRequest.Status }
        );
    }

    public async Task ForwardVideoGenerationDataAsync(VideoGenerationApprovedEto @event, CancellationToken cancellationToken = default)
    {
        string? videoInputJson = null;
        string? correlationId = null;

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var request = await customerContentRepository.GetFirstOrDefaultAsync(x => x.Id == @event.RefNormalizeRequestId, cancellationToken: cancellationToken);
            if (request == null) return;

            correlationId = request.CorrelationId;
            videoInputJson = BuildVideoInputJson(
                customerContentId: request.CustomerContentId,
                scrapeTitle: request.ScrapingResult?.Title,
                scrapeText: request.ScrapingResult?.Details,
                outlineScript: request.OutlineResult?.OutlinedData
            );
        }
        else if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var request = await analysisContentRepository.GetFirstOrDefaultAsync(x => x.Id == @event.RefNormalizeRequestId, cancellationToken: cancellationToken);
            if (request == null) return;

            correlationId = request.CorrelationId;
            var items = request.Items
                .Where(x => x.ScrapingStatus == StatusNames.Completed && x.OutlineStatus == StatusNames.Completed)
                .OrderBy(x => x.SortOrder)
                .ToList();

            videoInputJson = BuildAnalysisVideoInputJson(items);
        }

        if (string.IsNullOrWhiteSpace(videoInputJson)) return;

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: correlationId,
            eventMessage: new VideoGenerationDataForwardedEto
            {
                RefContentId = @event.RefContentId,
                RefContentType = @event.RefContentType,
                ScopeKey = @event.ScopeKey,
                NormalizeRequestId = @event.RefNormalizeRequestId,
                VideoInputJson = videoInputJson
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

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: ex.Message,
            reference: new
            {
                request.ScopeKey,
                ClientDomain = request.DomainName,
                ContentKey = request.ContentKey,
                RefContentId = request.CustomerContentId,
                RefNormalizedRequestId = request.Id
            },
            facility: step,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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
                RefContentId = request.AnalysisContentId,
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
                RefContentId = request.AnalysisContentId,
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

    private string BuildVideoInputJson(Guid customerContentId, string? scrapeTitle, string? scrapeText, string? outlineScript)
    {
        var audioItems = new[]
        {
            new
            {
                sortOrder = 1,
                customerContentId,
                text = scrapeText,
                title = scrapeTitle,
                outline = outlineScript
            }
        };

        return JsonConvert.SerializeObject(new { audioItems });
    }

    private string BuildAnalysisVideoInputJson(List<AnalysisNormalizedItem> items)
    {
        var audioItems = items
            .Select((item, index) => new
            {
                sortOrder = index + 1,
                customerContentId = item.CustomerContentId,
                contentKey = item.ContentKey,
                title = item.ScrapingResult?.Title,
                text = item.ScrapingResult?.Details,
                outline = item.OutlineResult?.OutlinedData
            })
            .ToList();

        return JsonConvert.SerializeObject(new { audioItems });
    }

    private async Task AppendIntroOutroToAnalysisItemsAsync(AnalysisContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        List<AnalysisNormalizedItem> orderedItems = [.. request.Items.OrderBy(x => x.SortOrder)];

        if (orderedItems.Count == 0) return;

        string introText = "İyi günler, günün öne çıkan haberleriyle karşınızdayız.";
        string outroText = "Günün öne çıkan gelişmelerini aktardık. Tekrar görüşmek üzere";

        CustomerVpSetting vpSetting = await customerVpSettingRepository.GetFirstOrDefaultAsync(
            x => x.ScopeKey == request.ScopeKey,
            cancellationToken: cancellationToken);

        bool canGenerateViaProvider = vpSetting is not null
                                      && !_normalizerSettings.SkipOutlineOperation
                                      && vpSetting.IsOutlineOperationActive
                                      && !string.IsNullOrWhiteSpace(vpSetting.AnalysisOutlineIntroPrompt)
                                      && !string.IsNullOrWhiteSpace(vpSetting.AnalysisOutlineOutroPrompt);

        if (canGenerateViaProvider)
        {
            KeyValuePair<bool, string> providerKeyResult = await customerVpSettingRepository.GetOutlineProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
            if (providerKeyResult.Key)
            {
                IOutlineProvider outlineProvider = outlineProviderResolver.Resolve(providerKeyResult.Value);

                OutlineCreateResponse introResponse = await outlineProvider.OutlineOperationAsync(new OutlineCreateRequest { OutlinePrompt = vpSetting.AnalysisOutlineIntroPrompt, OutlineInput = ".", EngineModel = "gpt-4.1-mini" });

                if (!string.IsNullOrWhiteSpace(introResponse?.OutlinedData))
                    introText = introResponse.OutlinedData;

                OutlineCreateResponse outroResponse = await outlineProvider.OutlineOperationAsync(new OutlineCreateRequest { OutlinePrompt = vpSetting.AnalysisOutlineOutroPrompt, OutlineInput = ".", EngineModel = "gpt-4.1-mini" });

                if (!string.IsNullOrWhiteSpace(outroResponse?.OutlinedData))
                    outroText = outroResponse.OutlinedData;
            }
        }

        AnalysisNormalizedItem firstItem = orderedItems[0];
        AnalysisNormalizedItem lastItem = orderedItems[^1];

        if (orderedItems.Count == 1)
        {
            firstItem.OutlineResult = new OutlineResult { OutlinedData = $"{introText} {firstItem.OutlineResult?.OutlinedData} {outroText}" };
        }
        else
        {
            firstItem.OutlineResult = new OutlineResult { OutlinedData = $"{introText} {firstItem.OutlineResult?.OutlinedData}" };

            lastItem.OutlineResult = new OutlineResult { OutlinedData = $"{lastItem.OutlineResult?.OutlinedData} {outroText}" };
        }
    }
}