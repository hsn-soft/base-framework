using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class AnalysisContentAppService : ApplicationServiceBase, IAnalysisContentAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IAnalysisContentRepository _analysisContentRepository;
    private readonly IClientVideoGenerationHistoryRepository _clientVideoGenerationHistoryRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IAppContentRepository _appContentRepository;
    private readonly IAppContentVisitRepository _appContentVisitRepository;

    public AnalysisContentAppService(IServiceProvider provider,
        IAnalysisContentRepository analysisContentRepository,
        IClientVideoGenerationHistoryRepository clientVideoGenerationHistoryRepository,
        IClientRepository clientRepository,
        IAppContentRepository appContentRepository,
        IAppContentVisitRepository appContentVisitRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _analysisContentRepository = analysisContentRepository;
        _clientVideoGenerationHistoryRepository = clientVideoGenerationHistoryRepository;
        _clientRepository = clientRepository;
        _appContentRepository = appContentRepository;
        _appContentVisitRepository = appContentVisitRepository;
    }

    public async Task SetNormalizedAnalysisReferenceAsync(Guid analysisContentId, Guid normalizedAnalysisId)
    {
        if (analysisContentId == Guid.Empty || normalizedAnalysisId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _analysisContentRepository.SetNormalizedAnalysisReferenceAsync(id: analysisContentId, normalizedAnalysisId: normalizedAnalysisId);
    }

    public async Task SetNormalizedResultAsync(Guid analysisContentId, Guid normalizedAnalysisId, bool isNormalizedSuccess, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty || normalizedAnalysisId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _analysisContentRepository.SetNormalizedContentResultAsync(
            id: analysisContentId,
            isNormalizedSuccess: isNormalizedSuccess,
            normalizedAnalysisId: normalizedAnalysisId
        );

        if (placed.OperationStatus == AnalysisContentOperationStates.NormalizedWaitForVideoGeneration)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Analysis Content normalized success",
                reference: new { placed.TenantId, placed.ClientId, RefContentId = placed.Id, placed.NormalizedAnalysisId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));

            // Add video generation history for client quote control
            await _clientVideoGenerationHistoryRepository.CreateAsync(clientId: placed.ClientId,
                videoGenerationDate: placed.AnalysisDate.Date,
                videoGenerationType: VideoGenerationTypes.AnalysisVideoGeneration,
                contentReferenceIds: placed.Id.ToString());

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationApprovedEto(
                    TenantId: placed.TenantId,
                    ClientId: placed.ClientId,
                    ReferenceContentType: ReferenceContentTypes.ANALYSIS_CONTENT,
                    ReferenceContentId: placed.Id
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Analysis Content normalized fail",
                reference: new { placed.TenantId, placed.ClientId, RefContentId = placed.Id, placed.NormalizedAnalysisId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetVideoGenerationRequestReferenceAsync(Guid analysisContentId, Guid videoRequestId)
    {
        if (analysisContentId == Guid.Empty || videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _analysisContentRepository.SetVideoRequestReferenceAsync(id: analysisContentId, videoRequestId: videoRequestId);
    }

    public async Task SetVideoGenerationResultAsync(Guid analysisContentId, Guid videoRequestId, bool isGenerateSuccess, string storageVideoUrl = null, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty || videoRequestId == Guid.Empty || (isGenerateSuccess && string.IsNullOrWhiteSpace(storageVideoUrl)))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _analysisContentRepository.SetVideoGenerationResultAsync(
            id: analysisContentId,
            isGenerateSuccess: isGenerateSuccess,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl);

        if (placed.OperationStatus == AnalysisContentOperationStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Video generation success",
                reference: new { placed.TenantId, placed.ClientId, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Video generation fail",
                reference: new { placed.TenantId, placed.ClientId, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid analysisContentId, string failedReason, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _analysisContentRepository.SetStatusToFailedAsync(id: analysisContentId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Analysis Content status fail: {failedReason ?? string.Empty}",
            reference: new { placed.TenantId, placed.ClientId, RefContentId = placed.Id },
            facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task AnalysisVideoGenerationQueryAsync(AnalysisVideoGenerationQueryEto input, string correlationId = null)
    {
        if (input?.AppClientId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var client = await _clientRepository.GetSingleOrDefaultAsync(x => x.Id == input.AppClientId);
        if (client == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "BEGIN");

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < client.DailyAnalysisVideoGenerationStartedUtcHour)
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}",
                client.DomainName, "SKIPPED", AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "END");
            return;
        }

        var analysisDate = DateTime.UtcNow.Date;

        // check client Analysis video generation quote available
        long clientDailyAnalysisContentCount = await _analysisContentRepository.GetCountAsync(x => x.ClientId == client.Id && x.AnalysisDate == analysisDate);
        long clientDailyAnalysisVideoGenerationLimit = client.DailyAnalysisVideoGenerationLimit - clientDailyAnalysisContentCount;
        var clientQuoteResult = clientDailyAnalysisVideoGenerationLimit <= 0
            ? new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED);
        if (clientQuoteResult.Key)
        {
            var contentIds = await _appContentRepository.GetClientDailyAnalysisContentIdsAsync(client.Id);
            if (contentIds is { Count: > 0 })
            {
                var contentVisitList = await _appContentVisitRepository.GetContentIdsVisitCountsAsync(contentIds: contentIds,
                    isMaxCountOrdered: true,
                    contentOrderedLimit: 5, // TODO: Get value from settings -> max 5 take
                    contentVisitedCountLimit: 3); // TODO: Get value from settings

                if (contentVisitList is { Count: >= 5 }) // TODO: Get value from settings
                {
                    bool operationSuccess;
                    string errorMessage = string.Empty;
                    var analysisContentId = Guid.CreateVersion7();
                    if (string.IsNullOrWhiteSpace(correlationId))
                    {
                        correlationId = Guid.CreateVersion7().ToString("N");
                    }

                    try
                    {
                        // Add analysis content record
                        var placed = await _analysisContentRepository.CreateAsync(
                            id: analysisContentId,
                            tenantId: client.TenantId,
                            clientId: client.Id,
                            analysisDate: analysisDate,
                            operationStatus: AnalysisContentOperationStates.CreatedWaitForNormalize,
                            correlationId: correlationId);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: $"Analysis Content created",
                            reference: new { placed.TenantId, placed.ClientId, AnalysisContentId = analysisContentId },
                            facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_CREATED,
                            correlationId: correlationId,
                            exception: null
                        ));

                        // Integration Event for TextNormalizerService
                        await EventBus.PublishAsync(correlationId: correlationId,
                            eventMessage: new AnalysisContentNormalizedStartedEto(
                                TenantId: placed.TenantId,
                                ClientId: placed.ClientId,
                                DomainName: client.DomainName,
                                AnalysisContentId: analysisContentId,
                                AppContentIdList: contentVisitList.Select(x => x.AppContentId).ToList(),
                                AnalysisDate: placed.AnalysisDate
                            ));

                        operationSuccess = true;
                    }
                    catch (Exception e)
                    {
                        operationSuccess = false;
                        errorMessage = e.Message;
                    }

                    if (operationSuccess)
                    {
                        _logger.LogInformation("Client[{ClientDomain}] | ANALYSIS CONTENT CREATED", client.DomainName);
                    }
                    else
                    {
                        _logger.LogError("Client[{ClientDomain}] | ANALYSIS CONTENT CREATION FAIL: {FailReason}", client.DomainName, errorMessage);

                        _logger.FrameworkErrorLog(LogHelper.Generate(
                            message: $"Analysis content video generation rejected: {errorMessage}",
                            reference: new { client.TenantId, ClientId = client.Id, AnalysisContentId = analysisContentId },
                            facility: AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED,
                            correlationId: correlationId,
                            exception: null
                        ));
                    }
                }
                else
                {
                    _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED",
                        "NOT_ENOUGH_VISIT_FOR_DAILY_ANALYSIS");
                }
            }
            else
            {
                _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", "NO_DAILY_ANALYSIS_CONTENT");
            }
        }
        else
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", clientQuoteResult.Value);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "END");
    }
}