using System.Diagnostics.CodeAnalysis;
using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Consts.Facilities;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Exceptions;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.ContentService.Domain.Settings;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.ContentService.Application.Services;

public sealed class CustomerContentAppService(
    IServiceProvider provider,
    IOptions<ContentOperationSettings> serviceSettings,
    ICustomerContentRepository customerContentRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IContentVideoGenerationLimitRepository contentVideoGenerationLimitRepository,
    ICustomerContentVisitRepository customerContentVisitRepository)
    : ApplicationServiceBase(provider), ICustomerContentAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly ContentOperationSettings _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));

    public async Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await customerContentRepository.GetSingleOrDefaultAsync<CustomerContentDto>(
            predicate: x => x.Id == id,
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync(GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        string scopeKey = null;
        if (pagedInput.CustomerId.HasValue && pagedInput.ProductType.HasValue)
        {
            scopeKey = ScopeKeyHelper.Generate(pagedInput.CustomerId.Value, pagedInput.ProductType.Value);
        }

        pagedInput.SearchText = StringHelper.SlugKeyNormalize(pagedInput.SearchText);
        pagedInput.SlugKey = StringHelper.SlugKeyNormalize(pagedInput.SlugKey);

        if (pagedInput.CreationTimeEnd.HasValue)
        {
            pagedInput.CreationTimeEnd = pagedInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (pagedInput.ReleaseTimeEnd.HasValue)
        {
            pagedInput.ReleaseTimeEnd = pagedInput.ReleaseTimeEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<CustomerContent>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.SlugKey.Contains(pagedInput.SearchText) : null)
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.SlugKey) ? e => e.SlugKey == pagedInput.SlugKey : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.ReleaseTimeStart.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime >= pagedInput.ReleaseTimeStart.Value : null)
            .And(pagedInput.ReleaseTimeEnd.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime < pagedInput.ReleaseTimeEnd.Value : null)
            .Build();

        var result = await customerContentRepository.GetPageListAsync<CustomerContentDto>(
            options: new PagedQueryOptions<CustomerContent>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? CustomerContentConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<CustomerContentDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<CustomerContentDto>> GetFilterListAsync(GetCustomerContentsFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        string scopeKey = null;
        if (filterInput.CustomerId.HasValue && filterInput.ProductType.HasValue)
        {
            scopeKey = ScopeKeyHelper.Generate(filterInput.CustomerId.Value, filterInput.ProductType.Value);
        }

        filterInput.SlugKey = StringHelper.SlugKeyNormalize(filterInput.SlugKey);

        if (filterInput.CreationTimeEnd.HasValue)
        {
            filterInput.CreationTimeEnd = filterInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (filterInput.ReleaseTimeEnd.HasValue)
        {
            filterInput.ReleaseTimeEnd = filterInput.ReleaseTimeEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<CustomerContent>()
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.SlugKey) ? e => e.SlugKey == filterInput.SlugKey : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.ReleaseTimeStart.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime >= filterInput.ReleaseTimeStart.Value : null)
            .And(filterInput.ReleaseTimeEnd.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime < filterInput.ReleaseTimeEnd.Value : null)
            .Build();

        return await customerContentRepository.GetListAsync<CustomerContentDto>(
            options: new ListQueryOptions<CustomerContent>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? CustomerContentConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<CustomerContentSearchDto>> GetSearchListAsync(GetCustomerContentsSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringHelper.SlugKeyNormalize(searchInput.SearchText);

        var filter = new FilterBuilder<CustomerContent>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.SlugKey.Contains(searchInput.SearchText) : null)
            .Build();

        return await customerContentRepository.GetListAsync<CustomerContentSearchDto>(
            options: new ListQueryOptions<CustomerContent>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? CustomerContentConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task SetCustomerContentNormalizedReferenceAsync(Guid customerContentId, Guid normalizedRequestId)
    {
        if (customerContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await customerContentRepository.SetCustomerContentNormalizedReferenceAsync(id: customerContentId, normalizedRequestId: normalizedRequestId);
    }

    public async Task SetCustomerContentNormalizedResultAsync(Guid customerContentId, Guid normalizedRequestId, bool isNormalizedSuccess, DateTime? releaseTime = null, string correlationId = null)
    {
        if (customerContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placedCustomerContent = await customerContentRepository.SetCustomerContentNormalizedResultAsync(
            id: customerContentId,
            isNormalizedSuccess: isNormalizedSuccess,
            normalizedRequestId: normalizedRequestId,
            releaseTime: releaseTime
        );

        if (placedCustomerContent.OperationStatus == CustomerContentOperationStates.NormalizedWaitForVideoGenerationApprove)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "CustomerContent normalized success",
                reference: new { placedCustomerContent.ScopeKey, RefContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_NORMALIZED_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));

            var checkResult = await CheckVideoGenerationApproveRules(placedCustomerContent.ScopeKey, placedCustomerContent.ReleaseTime);
            if (checkResult.Key)
            {
                await customerContentRepository.SetVideoGenerationApprovedAsync(id: placedCustomerContent.Id);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: "CustomerContent video generation approved",
                    reference: new { placedCustomerContent.ScopeKey, RefContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                    facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED,
                    correlationId: correlationId,
                    exception: null
                ));

                // Add video generation history for client quote control
                await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: placedCustomerContent.ScopeKey,
                    videoGenerationDate: placedCustomerContent.ReleaseTime?.Date ?? DateTime.UtcNow.Date,
                    videoGenerationType: VideoGenerationTypes.DirectVideoGeneration,
                    contentReferenceIds: placedCustomerContent.Id.ToString());

                // Integration Event for VideoGeneratorService
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new VideoGenerationApprovedEto(
                        ScopeKey: placedCustomerContent.ScopeKey,
                        ReferenceContentType: ReferenceContentTypes.CUSTOMER_CONTENT,
                        ReferenceContentId: placedCustomerContent.Id
                    ));
            }
            else
            {
                await customerContentRepository.SetVideoGenerationRejectedAsync(placedCustomerContent.Id, checkResult.Value);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: "CustomerContent video generation rejected",
                    reference: new { placedCustomerContent.ScopeKey, RefContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                    facility: checkResult.Value,
                    correlationId: correlationId,
                    exception: null
                ));
            }
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "CustomerContent normalized fail",
                reference: new { placedCustomerContent.ScopeKey, RefContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_NORMALIZED_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetCustomerContentVideoReferenceAsync(Guid customerContentId, Guid videoRequestId)
    {
        if (customerContentId == Guid.Empty || videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await customerContentRepository.SetCustomerContentVideoReferenceAsync(id: customerContentId, videoRequestId: videoRequestId);
    }

    public async Task SetCustomerContentVideoResultAsync(Guid customerContentId, Guid videoRequestId, bool isGenerateSuccess, string storageVideoUrl = null, string correlationId = null)
    {
        if (customerContentId == Guid.Empty || videoRequestId == Guid.Empty || (isGenerateSuccess && string.IsNullOrWhiteSpace(storageVideoUrl)))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await customerContentRepository.SetCustomerContentVideoResultAsync(
            id: customerContentId,
            isGenerateSuccess: isGenerateSuccess,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl);

        if (placed.OperationStatus == CustomerContentOperationStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Video generation success",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Video generation fail",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetCustomerContentStatusToFailedAsync(Guid customerContentId, string failedReason, string correlationId = null)
    {
        if (customerContentId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await customerContentRepository.SetCustomerContentStatusToFailedAsync(id: customerContentId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"CustomerContent status fail: {failedReason ?? string.Empty}",
            reference: new { placed.ScopeKey, RefContentId = placed.Id },
            facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task TrendVideoGenerationQueryAsync(TrendVideoGenerationQueryEto input, string correlationId = null)
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
        if (DateTime.UtcNow.Hour < customerVpSetting.DailyTrendVideoGenerationStartedUtcHour)
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}",
                customerVpSetting.DomainName, "SKIPPED", CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
            return;
        }

        // check client trend video generation quote available
        long clientDailyTrendVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(customerVpSetting.ScopeKey,
            DateTime.UtcNow.Date, VideoGenerationTypes.TrendVideoGeneration);

        long clientDailyTrendVideoGenerationLimit = customerVpSetting.DailyTrendVideoGenerationLimit - clientDailyTrendVideoHistoryCount;
        var clientQuoteResult = clientDailyTrendVideoGenerationLimit <= 0
            ? new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED);
        if (clientQuoteResult.Key)
        {
            var contentIds = await customerContentRepository.GetCustomerDailyTrendContentIdsAsync
            (
                scopeKey: customerVpSetting.ScopeKey,
                dailyTrendVideoWaitStatisticHour: customerVpSetting.DailyTrendVideoWaitStatisticHour
            );
            if (contentIds is { Count: > 0 })
            {
                var contentVisitList = await customerContentVisitRepository.GetContentIdsVisitCountsAsync(customerContentIds: contentIds,
                    isMaxCountOrdered: true,
                    customerContentOrderedLimit: clientDailyTrendVideoGenerationLimit,
                    customerContentVisitedCountLimit: customerVpSetting.DailyTrendVideoMinVisitCount);

                if (contentVisitList is { Count: > 0 })
                {
                    foreach (var contentVisit in contentVisitList)
                    {
                        var placedCustomerContent = await customerContentRepository.GetSingleOrDefaultAsync(x => x.Id == contentVisit.CustomerContentId);
                        if (placedCustomerContent is { OperationStatus: CustomerContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo })
                        {
                            bool operationSuccess;
                            string errorMessage = string.Empty;
                            try
                            {
                                await customerContentRepository.SetVideoGenerationApprovedAsync(id: contentVisit.CustomerContentId);

                                _logger.FrameworkInfoLog(LogHelper.Generate(
                                    message: "Trend content video generation approved",
                                    reference: new { placedCustomerContent.ScopeKey, CustomerContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                                    facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED,
                                    correlationId: placedCustomerContent.CorrelationId,
                                    exception: null
                                ));

                                // Add video generation history for client quote control
                                await contentVideoGenerationLimitRepository.CreateAsync(scopeKey: placedCustomerContent.ScopeKey,
                                    videoGenerationDate: placedCustomerContent.ReleaseTime?.Date ?? DateTime.UtcNow.Date,
                                    videoGenerationType: VideoGenerationTypes.TrendVideoGeneration,
                                    contentReferenceIds: placedCustomerContent.Id.ToString());

                                // Integration Event for TextNormalizerGeneratorService
                                await EventBus.PublishAsync(correlationId: placedCustomerContent.CorrelationId,
                                    eventMessage: new VideoGenerationApprovedEto(
                                        ScopeKey: placedCustomerContent.ScopeKey,
                                        ReferenceContentType: ReferenceContentTypes.CUSTOMER_CONTENT,
                                        ReferenceContentId: placedCustomerContent.Id
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
                                _logger.LogInformation("Client[{ClientDomain}] | CONTENT: {CustomerContentId}, VISIT: {VisitCount}",
                                    customerVpSetting.DomainName,
                                    contentVisit.CustomerContentId.ToString(),
                                    contentVisit.VisitCount.ToString());
                            }
                            else
                            {
                                _logger.LogError("Client[{ClientDomain}] | CONTENT: {CustomerContentId}, {OperationStatus} | {FailReason}", customerVpSetting.DomainName,
                                    contentVisit.CustomerContentId.ToString(), "FAIL", errorMessage);

                                _logger.FrameworkErrorLog(LogHelper.Generate(
                                    message: $"Trend content video generation rejected: {errorMessage}",
                                    reference: new { placedCustomerContent.ScopeKey, CustomerContentId = placedCustomerContent.Id, placedCustomerContent.NormalizedRequestId },
                                    facility: CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED,
                                    correlationId: placedCustomerContent.CorrelationId,
                                    exception: null
                                ));

                                await customerContentRepository.SetVideoGenerationRejectedAsync(id: contentVisit.CustomerContentId, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED);
                            }
                        }
                        else
                        {
                            _logger.LogError("Client[{ClientDomain}] | CONTENT: {CustomerContentId}, {OperationStatus} | {FailReason}",
                                customerVpSetting.DomainName,
                                contentVisit.CustomerContentId.ToString(),
                                "FAIL",
                                "CONTENT_INFO_NOT_FOUND");
                        }

                        // Loop period
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED", "NOT_ENOUGH_VISIT_FOR_DAILY_TREND");
                }
            }
            else
            {
                _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED", "NO_DAILY_TREND_CONTENT");
            }
        }
        else
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", customerVpSetting.DomainName, "SKIPPED", clientQuoteResult.Value);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
    }

    public async Task TestQueryRequestedAsync(TestQueryRequestedEto input, string correlationId = null)
    {
        await Task.Delay(10000);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new TestQueryCompletedEto(
                CustomerId: input.CustomerId
            ));
    }

    private async Task<KeyValuePair<bool, string>> CheckVideoGenerationApproveRules([NotNull] string scopeKey, DateTime? releaseTime)
    {
        if (_serviceSettings.SkipContentCheckOperation)
        {
            return new KeyValuePair<bool, string>(true, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED);
        }

        // Check content release time
        if (!releaseTime.HasValue || releaseTime.Value.ToUniversalTime().Date != DateTime.UtcNow.Date)
        {
            return new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_OLD_CONTENT);
        }

        // Get Client Details
        var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == scopeKey);
        if (customerVpSetting == null)
        {
            throw new CustomerVpSettingNotFoundException(L, scopeKey);
        }

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < customerVpSetting.DailyDirectVideoGenerationStartedUtcHour)
        {
            if (DateTime.UtcNow.Hour < releaseTime.Value.ToUniversalTime().Hour)
            {
                return new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            }
        }

        // Check client direct video generation limit
        if (customerVpSetting.DailyDirectVideoGenerationLimit <= 0)
        {
            return new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT);
        }

        // Check client direct video generation available
        long clientDailyDirectVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(scopeKey,
            releaseTime.Value.ToUniversalTime().Date, VideoGenerationTypes.DirectVideoGeneration);

        return customerVpSetting.DailyDirectVideoGenerationLimit - clientDailyDirectVideoHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, CustomerContentOperationFacilities.CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED);
    }
}