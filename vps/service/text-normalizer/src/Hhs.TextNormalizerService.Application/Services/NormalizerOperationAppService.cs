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

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentNormalizeRequestCreated,
                reference: new { existing.ScopeKey, RefContentId = existing.CustomerContentId, RefNormalizedRequestId = existing.Id },
                facility: EventNames.CustomerContentNormalizeRequestCreated,
                correlationId: existing.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: existing.CorrelationId,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto
                {
                    CustomerContentId = existing.CustomerContentId, // for content service update
                    CustomerContentNormalizeRequestId = existing.Id,
                    NormalizeStatus = existing.Status,
                    NormalizeCurrentStep = existing.CurrentStep,
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

            entity.Status = NormalizeStatusNames.Created;
            entity.CurrentStep = EventNames.CustomerContentNormalizeRequestCreated;

            entity.ScrapingStatus = ScrapingStatusNames.NotStarted;
            entity.OutlineStatus = OutlineStatusNames.NotStarted;

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
                correlationId: correlationId,
                eventMessage: new CustomerContentNormalizeRequestCreatedEto
                {
                    CustomerContentId = @event.CustomerContentId, // for content service update
                    CustomerContentNormalizeRequestId = requestId,
                    NormalizeStatus = entity.Status,
                    NormalizeCurrentStep = entity.CurrentStep,
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
            request.Status = NormalizeStatusNames.ScrapingStarted;
            request.CurrentStep = EventNames.CustomerContentScrapingStarted;

            request.ScrapingStatus = ScrapingStatusNames.Started;

            var claimed = await ReplaceCustomerContentAsync(request, cancellationToken,
                validPriorStatuses: [NormalizeStatusNames.Created, NormalizeStatusNames.WaitingRetry]);
            if (claimed == 0) return;

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
                correlationId: request.CorrelationId,
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

            request.Status = NormalizeStatusNames.ScrapingCompleted;
            request.CurrentStep = EventNames.CustomerContentScrapingCompleted;

            request.ScrapingStatus = ScrapingStatusNames.Completed;

            request.ScrapingResult = new ScrapingContentDataModel
            {
                Title = result.Title ?? string.Empty,
                Spot = result.Spot,
                ReleaseTimeUtc = result.ReleaseTimeUtc,
                Details = result.Details,
                ImageUrl = result.ImageUrl
            };
            request.LastError = null;

            var claimed = await ReplaceCustomerContentAsync(request, cancellationToken,
                validPriorStatuses: [NormalizeStatusNames.ScrapingStarted, NormalizeStatusNames.WaitingRetry]);
            if (claimed == 0) return;

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
                correlationId: request.CorrelationId,
                eventMessage: new CustomerContentScrapingCompletedEto
                {
                    // Update reference for Content Service
                    CustomerContentId = request.CustomerContentId,
                    CustomerContentNormalizeRequestId = request.Id,
                    ScrapedReleaseTimeUtc = request.ScrapingResult.ReleaseTimeUtc,
                    NormalizeStatus = request.Status,
                    NormalizeCurrentStep = request.CurrentStep
                }
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

    public async Task CompleteCustomerContentScrapingAsync(CustomerContentScrapingCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await customerContentRepository.GetByIdOrDefaultAsync(@event.CustomerContentNormalizeRequestId, cancellationToken: cancellationToken);
        if (request == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        try
        {
            request.Status = NormalizeStatusNames.OutlineStarted;
            request.CurrentStep = EventNames.CustomerContentOutlineStarted;

            request.OutlineStatus = OutlineStatusNames.Started;

            var claimed = await ReplaceCustomerContentAsync(request, cancellationToken,
                validPriorStatuses: [NormalizeStatusNames.ScrapingCompleted, NormalizeStatusNames.WaitingRetry]);
            if (claimed == 0) return;

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
                correlationId: request.CorrelationId,
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
                request.Status = NormalizeStatusNames.OutlineSkipped;
                request.CurrentStep = EventNames.CustomerContentOutlineSkipped;

                request.OutlineStatus = OutlineStatusNames.Skipped;

                var skipClaimed = await ReplaceCustomerContentAsync(request, cancellationToken,
                    validPriorStatuses: [NormalizeStatusNames.OutlineStarted, NormalizeStatusNames.WaitingRetry]);
                if (skipClaimed == 0) return;

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
                    facility: EventNames.CustomerContentOutlineSkipped,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: request.CorrelationId,
                    eventMessage: new NormalizerResultPublishedEto
                    {
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        NormalizeRequestId = request.Id,
                        NormalizeStatus = request.Status,
                        NormalizeCurrentStep = request.CurrentStep
                    }
                );

                return;
            }

            request.Status = NormalizeStatusNames.OutlineProviderRequestStarted;
            request.CurrentStep = EventNames.OutlineProviderRequestStarted;

            var claimed = await ReplaceCustomerContentAsync(request, cancellationToken,
                validPriorStatuses: [NormalizeStatusNames.OutlineStarted, NormalizeStatusNames.WaitingRetry]);
            if (claimed == 0) return;

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

            var (outlineInputText, outlineInputPrompt) = await BuildCustomerOutlineInputAsync(request, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: request.CorrelationId,
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
                correlationId: existing.CorrelationId,
                eventMessage: new AnalysisContentNormalizeRequestCreatedEto
                {
                    AnalysisContentId = existing.AnalysisContentId, AnalysisContentNormalizeRequestId = existing.Id, NormalizeStatus = existing.Status, NormalizeCurrentStep = existing.CurrentStep,
                }
            );

            return;
        }

        var requestId = Guid.CreateVersion7();
        var request = new AnalysisContentNormalizedRequest(
            requestId,
            @event.ScopeKey,
            @event.AnalysisContentId,
            @event.DomainName,
            correlationId);

        request.SourceEventId = eventId;
        request.Status = NormalizeStatusNames.Created;
        request.CurrentStep = EventNames.AnalysisContentCreated;
        request.Items = @event.Items.Select(x => new AnalysisNormalizedItem { CustomerContentId = x.CustomerContentId, SortOrder = x.SortOrder, ContentKey = x.ContentKey }).ToList();

        await analysisContentRepository.InsertAsync(request, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            correlationId: correlationId,
            eventMessage: new AnalysisContentNormalizeRequestCreatedEto
            {
                AnalysisContentId = @event.AnalysisContentId, AnalysisContentNormalizeRequestId = requestId, NormalizeStatus = request.Status, NormalizeCurrentStep = request.CurrentStep,
            }
        );
    }

    public async Task StartAnalysisContentNormalizeAsync(AnalysisContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await analysisContentRepository.GetFirstOrDefaultAsync(x => x.AnalysisContentId == @event.AnalysisContentId, cancellationToken: cancellationToken);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            if (item.ScrapingStatus == ScrapingStatusNames.Completed)
                continue;

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: request.CorrelationId,
                eventMessage: new AnalysisItemScrapingStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
            );
        }
    }

    public async Task StartAnalysisItemScrapingAsync(AnalysisItemScrapingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var request = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        if (request.Status == NormalizeStatusNames.Failed) return;

        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (!_normalizerSettings.ForceReScrapeForAnalysis)
        {
            var existingRequest = await customerContentRepository.GetByScopeKeyAndContentIdAsync(request.ScopeKey, item.CustomerContentId, cancellationToken);
            if (existingRequest?.ScrapingStatus == ScrapingStatusNames.Completed && existingRequest.ScrapingResult is not null)
            {
                var reuseClaimed = await UpdateAnalysisItemAsync(
                    request.Id,
                    item.CustomerContentId,
                    u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.ScrapingCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", ScrapingStatusNames.Completed)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingResult)}", existingRequest.ScrapingResult)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", default(string)),
                    cancellationToken,
                    validPriorScrapingStatuses: [ScrapingStatusNames.NotStarted, ScrapingStatusNames.WaitingRetry]
                );
                if (reuseClaimed == 0) return;

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.AnalysisItemScrapingCompleted,
                    reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId },
                    facility: EventNames.AnalysisItemScrapingCompleted,
                    correlationId: request.CorrelationId,
                    exception: null
                ));
                return;
            }
        }

        try
        {
            var claimed = await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.ScrapingStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", ScrapingStatusNames.Started),
                cancellationToken,
                validPriorScrapingStatuses: [ScrapingStatusNames.NotStarted, ScrapingStatusNames.WaitingRetry]
            );
            if (claimed == 0) return;

            var result = await scraper.ScrapeAsync(new ScraperRequestDto { DomainKey = request.DomainName, Path = item.ContentKey });

            await UpdateAnalysisItemAsync(
                request.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.ScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemScrapingCompleted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", ScrapingStatusNames.Completed)
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
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null),
                cancellationToken
            );

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AnalysisItemScrapingCompleted,
                reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId },
                facility: EventNames.AnalysisItemScrapingCompleted,
                correlationId: request.CorrelationId,
                exception: null
            ));
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

    public async Task StartAnalysisItemOutlineAsync(AnalysisItemOutlineStartedEto @event, CancellationToken cancellationToken = default)
    {
        var analysisContentNormalizedRequest = await GetAnalysisContentNormalizedRequestAsync(@event.AnalysisContentId);
        if (analysisContentNormalizedRequest.Status == NormalizeStatusNames.Failed) return;

        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        try
        {
            if (item.ScrapingStatus != ScrapingStatusNames.Completed || item.ScrapingResult is null)
            {
                await UpdateAnalysisItemAsync(
                    analysisContentNormalizedRequest.Id,
                    item.CustomerContentId,
                    u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.WaitingRetry)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.WaitingScraping)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", "ScrapingResult is required before outline.")
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.ErrorRescheduleDelaySeconds)),
                    cancellationToken
                );
                return;
            }

            var claimed = await UpdateAnalysisItemAsync(
                analysisContentNormalizedRequest.Id,
                item.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineProviderRequestStarted)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Started)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineStarted),
                cancellationToken,
                validPriorOutlineStatuses: [OutlineStatusNames.NotStarted, OutlineStatusNames.WaitingRetry]
            );
            if (claimed == 0) return;

            var (outlineInputText, outlineInputPrompt) = await BuildAnalysisItemOutlineInputAsync(analysisContentNormalizedRequest, item, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: analysisContentNormalizedRequest.CorrelationId,
                eventMessage: new OutlineProviderRequestStartedEto
                {
                    CustomerContentIdForItem = item.CustomerContentId,
                    RefContentType = ContentType.AnalysisContent,
                    RefNormalizedRequestId = analysisContentNormalizedRequest.Id,
                    ScopeKey = analysisContentNormalizedRequest.ScopeKey,
                    InputText = outlineInputText,
                    InputPrompt = outlineInputPrompt
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


    public async Task StartOutlineProviderRequestAsync(OutlineProviderRequestStartedEto @event, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default)
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

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.OutlineProviderRequestCompleted,
                    reference: new { @event.ScopeKey, @event.RefContentType, RefNormalizedRequestId = @event.RefNormalizedRequestId, @event.CustomerContentIdForItem },
                    facility: EventNames.OutlineProviderRequestCompleted,
                    correlationId: correlationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: correlationId,
                    eventMessage: new OutlineProviderCompletedEto { RefContentType = @event.RefContentType, RefNormalizedRequestId = @event.RefNormalizedRequestId, CustomerContentIdForItem = @event.CustomerContentIdForItem, OutlinedData = response.OutlinedData }
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("ProviderTrackId is required for async outline provider.");

            await SaveOutlinePollingStateAsync(@event, response.ProviderTrackId, correlationId);
        }
        catch (Exception ex)
        {
            await HandleOutlineProviderRequestExceptionAsync(@event, ex);
        }
    }

    private async Task SaveOutlinePollingStateAsync(OutlineProviderRequestStartedEto @event, string providerTrackId, [CanBeNull] string correlationId = null)
    {
        if (@event.RefContentType == ContentType.None) throw new InvalidOperationException("RefContentType is required.");

        if (@event.RefContentType == ContentType.CustomerContent)
        {
            var request = await customerContentRepository.GetByIdAsync(@event.RefNormalizedRequestId);

            request.OutlineProviderTrackId = providerTrackId;

            request.Status = NormalizeStatusNames.OutlineProviderRequestPolling;
            request.CurrentStep = EventNames.OutlineProviderPollingStarted;

            request.OutlineStatus = OutlineStatusNames.Polling;

            request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds);

            await ReplaceCustomerContentAsync(request);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.OutlineProviderPollingStarted,
                reference: new { request.ScopeKey, RefContentId = request.CustomerContentId, RefNormalizedRequestId = request.Id, request.NextOutlinePollAtUtc },
                facility: EventNames.OutlineProviderPollingStarted,
                correlationId: request.CorrelationId,
                exception: null
            ));
            return;
        }

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline polling.");

        await UpdateAnalysisItemAsync(
            @event.RefNormalizedRequestId,
            @event.CustomerContentIdForItem.Value,
            u => u
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineProviderRequestPolling)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineProviderTrackId)}", providerTrackId)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Polling)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(outlinePollingSettings.IntervalSeconds)),
            CancellationToken.None
        );

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.OutlineProviderPollingStarted,
            reference: new { RefNormalizedRequestId = @event.RefNormalizedRequestId, @event.CustomerContentIdForItem },
            facility: EventNames.OutlineProviderPollingStarted,
            correlationId: correlationId,
            exception: null
        ));
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

            if (customerContentNormalizedRequest.Status == NormalizeStatusNames.Completed ||
                customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished ||
                customerContentNormalizedRequest.OutlineStatus == OutlineStatusNames.Completed)
                return;

            customerContentNormalizedRequest.Status = NormalizeStatusNames.OutlineCompleted;
            customerContentNormalizedRequest.CurrentStep = EventNames.CustomerContentOutlineCompleted;

            customerContentNormalizedRequest.OutlineStatus = OutlineStatusNames.Completed;

            customerContentNormalizedRequest.OutlineResult = new OutlineResult { OutlinedData = @event.OutlinedData };

            var customerClaimed = await ReplaceCustomerContentAsync(customerContentNormalizedRequest, cancellationToken,
                validPriorStatuses: [NormalizeStatusNames.OutlineProviderRequestStarted, NormalizeStatusNames.OutlineProviderRequestPolling, NormalizeStatusNames.OutlineProviderRequestCompleted, NormalizeStatusNames.WaitingRetry]);
            if (customerClaimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.CustomerContentOutlineCompleted,
                reference: new { customerContentNormalizedRequest.ScopeKey, RefContentId = customerContentNormalizedRequest.CustomerContentId, RefNormalizedRequestId = customerContentNormalizedRequest.Id },
                facility: EventNames.CustomerContentOutlineCompleted,
                correlationId: customerContentNormalizedRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: customerContentNormalizedRequest.CorrelationId,
                eventMessage: new CustomerContentOutlineCompletedEto { RefContentId = customerContentNormalizedRequest.CustomerContentId, RefContentType = ContentType.CustomerContent, Script = @event.OutlinedData }
            );
            return;
        }

        var analysisContentNormalizedRequest = await analysisContentRepository.GetByIdAsync(@event.RefNormalizedRequestId, cancellationToken: cancellationToken);

        if (@event.CustomerContentIdForItem is null)
            throw new InvalidOperationException("CustomerContentIdForItem is required for analysis outline completion.");

        var item = analysisContentNormalizedRequest.Items.First(x => x.CustomerContentId == @event.CustomerContentIdForItem);

        if (item.Status == NormalizeStatusNames.OutlineCompleted || item.OutlineStatus == OutlineStatusNames.Completed)
            return;

        var claimed = await UpdateAnalysisItemAsync(
            analysisContentNormalizedRequest.Id,
            item.CustomerContentId,
            u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.AnalysisItemOutlineCompleted)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Completed)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", new OutlineResult { OutlinedData = @event.OutlinedData })
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null),
            CancellationToken.None,
            validPriorOutlineStatuses: [OutlineStatusNames.Started, OutlineStatusNames.Polling, OutlineStatusNames.ProviderCompleted, OutlineStatusNames.WaitingRetry]
        );
        if (claimed == 0) return;

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.AnalysisItemOutlineCompleted,
            reference: new { analysisContentNormalizedRequest.ScopeKey, RefContentId = analysisContentNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = analysisContentNormalizedRequest.Id, item.CustomerContentId },
            facility: EventNames.AnalysisItemOutlineCompleted,
            correlationId: analysisContentNormalizedRequest.CorrelationId,
            exception: null
        ));
    }

    public async Task CompleteCustomerContentOutlineAsync(CustomerContentOutlineCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var customerContentNormalizedRequest = await customerContentRepository.GetFirstOrDefaultAsync(
            x => x.CustomerContentId == @event.RefContentId,
            cancellationToken: cancellationToken);

        if (customerContentNormalizedRequest.Status == NormalizeStatusNames.Completed ||
            customerContentNormalizedRequest.CurrentStep == EventNames.NormalizerResultPublished)
            return;

        customerContentNormalizedRequest.Status = NormalizeStatusNames.Completed;
        customerContentNormalizedRequest.CurrentStep = EventNames.NormalizerResultPublished;

        var claimed = await ReplaceCustomerContentAsync(customerContentNormalizedRequest, cancellationToken,
            validPriorStatuses: [NormalizeStatusNames.OutlineCompleted, NormalizeStatusNames.WaitingRetry]);
        if (claimed == 0) return;

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.NormalizerResultPublished,
            reference: new { customerContentNormalizedRequest.ScopeKey, RefContentId = customerContentNormalizedRequest.CustomerContentId, RefNormalizedRequestId = customerContentNormalizedRequest.Id },
            facility: EventNames.NormalizerResultPublished,
            correlationId: customerContentNormalizedRequest.CorrelationId,
            exception: null
        ));

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            correlationId: customerContentNormalizedRequest.CorrelationId,
            eventMessage: new NormalizerResultPublishedEto
            {
                RefContentId = customerContentNormalizedRequest.CustomerContentId,
                RefContentType = ContentType.CustomerContent,
                NormalizeRequestId = customerContentNormalizedRequest.Id,
                NormalizeStatus = customerContentNormalizedRequest.Status,
                NormalizeCurrentStep = customerContentNormalizedRequest.CurrentStep
            }
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
                scrapeImageUrl: request.ScrapingResult?.ImageUrl,
                scrapeTitle: request.ScrapingResult?.Title,
                outlineScript: request.OutlineResult?.OutlinedData
            );
        }
        else if (@event.RefContentType == ContentType.AnalysisContent)
        {
            var request = await analysisContentRepository.GetFirstOrDefaultAsync(x => x.Id == @event.RefNormalizeRequestId, cancellationToken: cancellationToken);
            if (request == null) return;

            correlationId = request.CorrelationId;
            var items = request.Items
                .Where(x => x.ScrapingStatus == ScrapingStatusNames.Completed && x.OutlineStatus == OutlineStatusNames.Completed)
                .OrderBy(x => x.SortOrder)
                .ToList();

            videoInputJson = BuildAnalysisVideoInputJson(items);
        }

        if (string.IsNullOrWhiteSpace(videoInputJson)) return;

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoGenerationDataForwarded,
            reference: new { @event.ScopeKey, RefContentId = @event.RefContentId, RefNormalizedRequestId = @event.RefNormalizeRequestId },
            facility: EventNames.VideoGenerationDataForwarded,
            correlationId: correlationId,
            exception: null
        ));

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


    private Task<AnalysisContentNormalizedRequest> GetAnalysisContentNormalizedRequestAsync(Guid analysisContentId)
    {
        return analysisContentRepository.GetFirstOrDefaultAsync(x => x.AnalysisContentId == analysisContentId);
    }

    /// <summary>
    /// Single source of truth for the outline provider's input (customer path) — used both by the
    /// original call site and by NormalizerOperationRetryWorkerService.RetryCustomerRequestsAsync
    /// when re-publishing OutlineProviderRequestStartedEto on retry, so a retried outline call never
    /// silently loses the customer's outline prompt or falls back to incomplete input text.
    /// </summary>
    internal async Task<(string InputText, string InputPrompt)> BuildCustomerOutlineInputAsync(CustomerContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        string outlineInputPrompt = await customerVpSettingRepository.GetContentOutlinePromptByScopeKeyAsync(request.ScopeKey, cancellationToken);

        string outlineInputText = string.Format("{0} {1} {2}",
            request.ScrapingResult.Title,
            request.ScrapingResult.Spot ?? string.Empty,
            request.ScrapingResult.Details ?? string.Empty
        );

        return (outlineInputText, outlineInputPrompt);
    }

    /// <summary>
    /// Single source of truth for the outline provider's input (analysis-item path) — used both by
    /// the original call site and by NormalizerOperationRetryWorkerService.RetryAnalysisRequestsAsync
    /// when re-publishing OutlineProviderRequestStartedEto on retry, so a retried outline call never
    /// silently loses the customer's outline prompt or falls back to incomplete input text.
    /// </summary>
    internal async Task<(string InputText, string InputPrompt)> BuildAnalysisItemOutlineInputAsync(AnalysisContentNormalizedRequest request, AnalysisNormalizedItem item, CancellationToken cancellationToken)
    {
        CustomerVpSetting analysisVpSetting = await customerVpSettingRepository.GetFirstOrDefaultAsync(
            x => x.ScopeKey == request.ScopeKey,
            cancellationToken: cancellationToken);

        string outlineInputText = analysisVpSetting?.IsForceContentDetailInAnalyseActive == true
            ? item.ScrapingResult.Details
            : item.ScrapingResult.Title.Replace(":", "") + " : " + (item.ScrapingResult.Spot ?? item.ScrapingResult.Details);

        return (outlineInputText, analysisVpSetting?.AnalysisOutlineContentPrompt);
    }

    /// <summary>
    /// Full-field conditional replace for CustomerContentNormalizedRequest, mirroring the CAS
    /// pattern used throughout video-generator: when <paramref name="validPriorStatuses"/> is
    /// supplied, the write only takes effect if the row's current Status is still one of the
    /// expected prior values — guarding against a concurrent duplicate delivery of the same event
    /// re-applying the same transition and double-publishing the step's completion event. Returns
    /// the affected row count (0 or 1); callers must check it and no-op on 0.
    /// </summary>
    private Task<long> ReplaceCustomerContentAsync(CustomerContentNormalizedRequest request, CancellationToken cancellationToken = default, IReadOnlyCollection<string> validPriorStatuses = null)
    {
        Expression<Func<CustomerContentNormalizedRequest, bool>> predicate = validPriorStatuses is null
            ? x => x.Id == request.Id
            : x => x.Id == request.Id && validPriorStatuses.Contains(x.Status);

        var update = Builders<CustomerContentNormalizedRequest>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.CurrentStep, request.CurrentStep)
            .Set(x => x.ScrapingStatus, request.ScrapingStatus)
            .Set(x => x.ScrapingResult, request.ScrapingResult)
            .Set(x => x.OutlineStatus, request.OutlineStatus)
            .Set(x => x.OutlineResult, request.OutlineResult)
            .Set(x => x.OutlineProviderTrackId, request.OutlineProviderTrackId)
            .Set(x => x.NextOutlinePollAtUtc, request.NextOutlinePollAtUtc)
            .Set(x => x.OutlinePollingCount, request.OutlinePollingCount)
            .Set(x => x.RetryCount, request.RetryCount)
            .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc)
            .Set(x => x.LastError, request.LastError);

        return customerContentRepository.UpdateByExpressionAsync(predicate, u => update, cancellationToken: cancellationToken);
    }

    private async Task FailCustomerAsync(CustomerContentNormalizedRequest request, string step, Exception ex, bool retryable)
    {
        request.Status = NormalizeStatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        await ReplaceCustomerContentAsync(request);

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

        request.Status = NormalizeStatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceCustomerContentAsync(request);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                RefContentId = request.CustomerContentId,
                RefNormalizedRequestId = request.Id,
                FailedStep = step,
                request.RetryCount,
                request.NextRetryAtUtc
            },
            facility: EventNames.RetryScheduled,
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
            var baseUpdate = u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.WaitingRetry)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.RetryCount)}", retryCount)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", nextRetryAtUtc);

            if (step == EventNames.AnalysisItemScrapingStarted)
            {
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", ScrapingStatusNames.WaitingRetry);
            }

            if (step is EventNames.AnalysisItemOutlineStarted
                or EventNames.OutlineProviderRequestStarted
                or EventNames.OutlineProviderPollingStarted)
            {
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.WaitingRetry);
            }

            return baseUpdate;
        });

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            updateFunc,
            CancellationToken.None
        );

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                RefContentId = request.AnalysisContentId,
                RefNormalizedRequestId = request.Id,
                item.CustomerContentId,
                FailedStep = step,
                retryCount,
                nextRetryAtUtc
            },
            facility: EventNames.RetryScheduled,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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
            var baseUpdate = u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.Failed)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", step)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null);

            if (step == EventNames.AnalysisItemScrapingStarted)
                // Scraping failed, so this item's outline will never be attempted either — mark it
                // Failed too so the outline-completeness gate (worker) doesn't wait on it forever.
                baseUpdate = baseUpdate
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.ScrapingStatus)}", ScrapingStatusNames.Failed)
                    .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Failed);

            if (step is EventNames.AnalysisItemOutlineStarted
                or EventNames.OutlineProviderRequestStarted
                or EventNames.OutlineProviderPollingStarted)
                baseUpdate = baseUpdate.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Failed);

            return baseUpdate;
        });

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            updateFunc,
            CancellationToken.None
        );

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: step,
            reference: new
            {
                request.ScopeKey,
                RefContentId = request.AnalysisContentId,
                RefNormalizedRequestId = request.Id,
                item.CustomerContentId,
                retryable
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
                RefContentId = request.AnalysisContentId,
                RefContentType = ContentType.AnalysisContent,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = retryable
            }
        );
    }

    /// <summary>
    /// Positional per-item update on AnalysisContentNormalizedRequest.Items. When
    /// <paramref name="validPriorScrapingStatuses"/> or <paramref name="validPriorOutlineStatuses"/>
    /// is supplied, the write only takes effect if the matched item's current ScrapingStatus/
    /// OutlineStatus is still one of the expected prior values — the same CAS-guard-before-publish
    /// pattern used for the parent-level entities, applied at the array-element level so a
    /// concurrent duplicate delivery of the same item's event can't double-apply the transition or
    /// double-publish its completion event. Returns the affected row count (0 or 1); callers that
    /// pass a guard must check it and no-op on 0.
    /// </summary>
    private Task<long> UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        Func<UpdateDefinitionBuilder<AnalysisContentNormalizedRequest>, UpdateDefinition<AnalysisContentNormalizedRequest>> updateFunc,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<string> validPriorScrapingStatuses = null,
        IReadOnlyCollection<string> validPriorOutlineStatuses = null)
    {
        Expression<Func<AnalysisContentNormalizedRequest, bool>> predicate;

        if (validPriorScrapingStatuses is not null)
        {
            predicate = x => x.Id == analysisRequestId &&
                             x.Items.Any(i => i.CustomerContentId == customerContentId && validPriorScrapingStatuses.Contains(i.ScrapingStatus));
        }
        else if (validPriorOutlineStatuses is not null)
        {
            predicate = x => x.Id == analysisRequestId &&
                             x.Items.Any(i => i.CustomerContentId == customerContentId && validPriorOutlineStatuses.Contains(i.OutlineStatus));
        }
        else
        {
            predicate = x => x.Id == analysisRequestId && x.Items.Any(i => i.CustomerContentId == customerContentId);
        }

        return analysisContentRepository.UpdateByExpressionAsync(predicate, updateFunc, cancellationToken);
    }

    private string BuildVideoInputJson(Guid customerContentId, string? scrapeImageUrl, string? scrapeTitle, string? outlineScript)
    {
        var audioItems = new[]
        {
            new
            {
                sortOrder = 1,
                customerContentId,
                encodedImageUrl = EncodeOrNull(scrapeImageUrl),
                encodedTitle = EncodeOrNull(scrapeTitle),
                encodedOutlineData = EncodeOrNull(outlineScript)
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
                encodedImageUrl = EncodeOrNull(item.ScrapingResult?.ImageUrl),
                encodedTitle = EncodeOrNull(item.ScrapingResult?.Title),
                encodedOutlineData = EncodeOrNull(item.OutlineResult?.OutlinedData)
            })
            .ToList();

        return JsonConvert.SerializeObject(new { audioItems });
    }

    private static string? EncodeOrNull(string? value) => value is null ? null : StringHelper.Base64Encode(value);

    /// <summary>
    /// Called by NormalizerOperationRetryWorkerService.AdvanceReadyAnalysisContentsToResultAsync
    /// after it has atomically claimed the parent, immediately before publishing the result — so
    /// this persists its own per-item writes (there's no longer a caller-side whole-document
    /// replace to piggy-back on).
    /// </summary>
    internal async Task AppendIntroOutroToAnalysisItemsAsync(AnalysisContentNormalizedRequest request, CancellationToken cancellationToken)
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

            await UpdateAnalysisItemAsync(
                request.Id,
                firstItem.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", firstItem.OutlineResult),
                cancellationToken
            );
        }
        else
        {
            firstItem.OutlineResult = new OutlineResult { OutlinedData = $"{introText} {firstItem.OutlineResult?.OutlinedData}" };
            lastItem.OutlineResult = new OutlineResult { OutlinedData = $"{lastItem.OutlineResult?.OutlinedData} {outroText}" };

            await UpdateAnalysisItemAsync(
                request.Id,
                firstItem.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", firstItem.OutlineResult),
                cancellationToken
            );

            await UpdateAnalysisItemAsync(
                request.Id,
                lastItem.CustomerContentId,
                u => u.Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineResult)}", lastItem.OutlineResult),
                cancellationToken
            );
        }
    }
}