using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ContentDomain.Consts.Facilities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class AnalysisContentAppService(
    IServiceProvider provider,
    IAnalysisContentRepository analysisContentRepository,
    IContentVideoGenerationLimitRepository contentVideoGenerationLimitRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    ICustomerContentRepository customerContentRepository,
    ICustomerContentVisitRepository customerContentVisitRepository)
    : ApplicationServiceBase(provider), IAnalysisContentAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task SetAnalysisContentNormalizedReferenceAsync(Guid analysisContentId, Guid normalizedRequestId)
    {
        if (analysisContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await analysisContentRepository.SetAnalysisContentNormalizedReferenceAsync(id: analysisContentId, normalizedRequestId: normalizedRequestId);
    }

    public async Task SetAnalysisContentNormalizedResultAsync(Guid analysisContentId, Guid normalizedRequestId, bool isNormalizedSuccess, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placedAnanlysisContent = await analysisContentRepository.SetAnalysisContentNormalizedResultAsync(
            id: analysisContentId,
            isNormalizedSuccess: isNormalizedSuccess,
            normalizedRequestId: normalizedRequestId
        );

        if (placedAnanlysisContent.OperationStatus == AnalysisContentOperationStates.NormalizedWaitForVideoGeneration)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Analysis Content normalized success",
                reference: new { placedAnanlysisContent.ScopeKey, RefContentId = placedAnanlysisContent.Id, NormalizedAnalysisId = placedAnanlysisContent.NormalizedRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));

            // Add video generation history for client quote control
            await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: placedAnanlysisContent.ScopeKey,
                videoGenerationDate: placedAnanlysisContent.AnalysisDate.Date,
                videoGenerationType: VideoGenerationTypes.AnalysisVideoGeneration,
                contentReferenceIds: placedAnanlysisContent.Id.ToString());

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationApprovedEto(
                    ScopeKey: placedAnanlysisContent.ScopeKey,
                    ReferenceContentType: ReferenceContentTypes.ANALYSIS_CONTENT,
                    ReferenceContentId: placedAnanlysisContent.Id
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Analysis Content normalized fail",
                reference: new { placedAnanlysisContent.ScopeKey, RefContentId = placedAnanlysisContent.Id, NormalizedAnalysisId = placedAnanlysisContent.NormalizedRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_NORMALIZED_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetAnalysisContentVideoReferenceAsync(Guid analysisContentId, Guid videoRequestId)
    {
        if (analysisContentId == Guid.Empty || videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await analysisContentRepository.SetAnalysisContentVideoReferenceAsync(id: analysisContentId, videoRequestId: videoRequestId);
    }

    public async Task SetAnalysisContentVideoResultAsync(Guid analysisContentId, Guid videoRequestId, bool isGenerateSuccess, string storageVideoUrl = null, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty || videoRequestId == Guid.Empty || (isGenerateSuccess && string.IsNullOrWhiteSpace(storageVideoUrl)))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await analysisContentRepository.SetAnalysisContentVideoResultAsync(
            id: analysisContentId,
            isGenerateSuccess: isGenerateSuccess,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl);

        if (placed.OperationStatus == AnalysisContentOperationStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Video generation success",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Video generation fail",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_VIDEO_GENERATION_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetAnalysisContentStatusToFailedAsync(Guid analysisContentId, string failedReason, string correlationId = null)
    {
        if (analysisContentId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await analysisContentRepository.SetAnalysisContentStatusToFailedAsync(id: analysisContentId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Analysis Content status fail: {failedReason ?? string.Empty}",
            reference: new { placed.ScopeKey, RefContentId = placed.Id },
            facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task AnalysisVideoGenerationQueryAsync(AnalysisVideoGenerationQueryEto input, string correlationId = null)
    {
        if (input?.ScopeKey == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == input.ScopeKey);
        if (customerVpSetting == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "BEGIN");

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < customerVpSetting.DailyAnalysisVideoGenerationStartedUtcHour)
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}",
                customerVpSetting.DomainName, "SKIPPED", CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
            return;
        }

        var analysisDate = DateTime.UtcNow.Date;

        // check client Analysis video generation quote available
        long clientDailyAnalysisContentCount = await analysisContentRepository.GetCountAsync(x => x.ScopeKey == customerVpSetting.ScopeKey && x.AnalysisDate == analysisDate);
        long clientDailyAnalysisVideoGenerationLimit = customerVpSetting.DailyAnalysisVideoGenerationLimit - clientDailyAnalysisContentCount;
        var clientQuoteResult = clientDailyAnalysisVideoGenerationLimit <= 0
            ? new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED);
        if (clientQuoteResult.Key)
        {
            var customerContentIds = await customerContentRepository.GetCustomerDailyAnalysisContentIdsAsync(customerVpSetting.ScopeKey);
            if (customerContentIds is { Count: > 0 })
            {
                var contentVisitList = await customerContentVisitRepository.GetContentIdsVisitCountsAsync(customerContentIds: customerContentIds,
                    isMaxCountOrdered: true,
                    customerContentOrderedLimit: 5, // TODO: Get value from settings -> max 5 take
                    customerContentVisitedCountLimit: 3); // TODO: Get value from settings

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
                        var placed = await analysisContentRepository.CreateAsync(
                            id: analysisContentId,
                            customerId: Guid.Parse(input.ScopeKey.Split(":")[0]),
                            productType: ProductTypes.VideoPlatform,
                            analysisDate: analysisDate,
                            operationStatus: AnalysisContentOperationStates.CreatedWaitForNormalize,
                            correlationId: correlationId);

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: $"Analysis Content created",
                            reference: new { placed.ScopeKey, AnalysisContentId = analysisContentId },
                            facility: AnalysisContentOperationFacilities.ANALYSIS_CONTENT_CREATED,
                            correlationId: correlationId,
                            exception: null
                        ));

                        // Integration Event for TextNormalizerService
                        await EventBus.PublishAsync(correlationId: correlationId,
                            eventMessage: new AnalysisContentNormalizedStartedEto(
                                ScopeKey: customerVpSetting.ScopeKey,
                                DomainName: customerVpSetting.DomainName,
                                AnalysisContentId: analysisContentId,
                                CustomerContentIdList: contentVisitList.Select(x => x.CustomerContentId).ToList(),
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
                        _logger.LogInformation("Client[{ClientDomain}] | ANALYSIS CONTENT CREATED", customerVpSetting.DomainName);
                    }
                    else
                    {
                        _logger.LogError("Client[{ClientDomain}] | ANALYSIS CONTENT CREATION FAIL: {FailReason}", customerVpSetting.DomainName, errorMessage);

                        _logger.FrameworkErrorLog(LogHelper.Generate(
                            message: $"Analysis content video generation rejected: {errorMessage}",
                            reference: new { customerVpSetting.ScopeKey, AnalysisContentId = analysisContentId },
                            facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED,
                            correlationId: correlationId,
                            exception: null
                        ));
                    }
                }
                else
                {
                    _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED",
                        "NOT_ENOUGH_VISIT_FOR_DAILY_ANALYSIS");
                }
            }
            else
            {
                _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED", "NO_DAILY_ANALYSIS_CONTENT");
            }
        }
        else
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED", clientQuoteResult.Value);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
    }
}