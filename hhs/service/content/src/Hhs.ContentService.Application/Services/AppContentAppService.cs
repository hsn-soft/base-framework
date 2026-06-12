using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.CustomerDomain.Exceptions;
using Hhs.ContentService.Domain.CustomerDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.Settings;
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

public sealed class AppContentAppService : ApplicationServiceBase, IAppContentAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly ContentOperationSettings _serviceSettings;
    private readonly IAppContentRepository _appContentRepository;
    private readonly ICustomerContentSettingRepository _customerContentSettingRepository;
    private readonly ICustomerVideoGenerationHistory _clientVideoGenerationHistoryRepository;
    private readonly IAppContentVisitRepository _appContentVisitRepository;

    public AppContentAppService(IServiceProvider provider,
        IOptions<ContentOperationSettings> serviceSettings,
        IAppContentRepository appContentRepository,
        ICustomerContentSettingRepository customerContentSettingRepository,
        ICustomerVideoGenerationHistory clientVideoGenerationHistoryRepository,
        IAppContentVisitRepository appContentVisitRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
        _appContentRepository = appContentRepository;
        _customerContentSettingRepository = customerContentSettingRepository;
        _clientVideoGenerationHistoryRepository = clientVideoGenerationHistoryRepository;
        _appContentVisitRepository = appContentVisitRepository;
    }

    public async Task<AppContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appContentRepository.GetSingleOrDefaultAsync<AppContentDto>(
            predicate: x => x.Id == id,
            // includeEntity: q => q.Include(x => x.Client),
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<AppContentDto>> GetPagedListAsync(GetAppContentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
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

        var filter = new FilterBuilder<AppContent>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.SlugKey.Contains(pagedInput.SearchText) : null)
            // .And(pagedInput.CustomerId.HasValue ? e => e.CustomerId == pagedInput.CustomerId.Value : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.SlugKey) ? e => e.SlugKey == pagedInput.SlugKey : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.ReleaseTimeStart.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime >= pagedInput.ReleaseTimeStart.Value : null)
            .And(pagedInput.ReleaseTimeEnd.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime < pagedInput.ReleaseTimeEnd.Value : null)
            .Build();

        var result = await _appContentRepository.GetPageListAsync<AppContentDto>(
            options: new PagedQueryOptions<AppContent>
            {
                Filter = filter,
                // IncludeEntity = q => q.Include(x => x.Client),
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? AppContentConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<AppContentDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<AppContentDto>> GetFilterListAsync(GetAppContentsFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
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

        var filter = new FilterBuilder<AppContent>()
            // .And(filterInput.CustomerId.HasValue ? e => e.CustomerId == filterInput.CustomerId.Value : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.SlugKey) ? e => e.SlugKey == filterInput.SlugKey : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.ReleaseTimeStart.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime >= filterInput.ReleaseTimeStart.Value : null)
            .And(filterInput.ReleaseTimeEnd.HasValue ? e => e.ReleaseTime != null && e.ReleaseTime < filterInput.ReleaseTimeEnd.Value : null)
            .Build();

        return await _appContentRepository.GetListAsync<AppContentDto>(
            options: new ListQueryOptions<AppContent>
            {
                Filter = filter,
                // IncludeEntity = q => q.Include(x => x.Client),
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AppContentConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<AppContentSearchDto>> GetSearchListAsync(GetAppContentsSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringHelper.SlugKeyNormalize(searchInput.SearchText);

        var filter = new FilterBuilder<AppContent>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.SlugKey.Contains(searchInput.SearchText) : null)
            .Build();

        return await _appContentRepository.GetListAsync<AppContentSearchDto>(
            options: new ListQueryOptions<AppContent>
            {
                Filter = filter,
                // IncludeEntity = q => q.Include(x => x.Client),
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? AppContentConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task SetNormalizedRequestReferenceAsync(Guid appContentId, Guid normalizedRequestId)
    {
        if (appContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _appContentRepository.SetNormalizedRequestReferenceAsync(id: appContentId, normalizedRequestId: normalizedRequestId);
    }

    public async Task SetNormalizedResultAsync(Guid appContentId, Guid normalizedRequestId, bool isNormalizedSuccess, DateTime? releaseTime = null, string correlationId = null)
    {
        if (appContentId == Guid.Empty || normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _appContentRepository.SetNormalizedContentResultAsync(
            id: appContentId,
            isNormalizedSuccess: isNormalizedSuccess,
            normalizedRequestId: normalizedRequestId,
            releaseTime: releaseTime
        );

        if (placed.OperationStatus == AppContentOperationStates.NormalizedWaitForVideoGenerationApprove)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "AppContent normalized success",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.NormalizedRequestId },
                facility: AppContentOperationFacilities.APP_CONTENT_NORMALIZED_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));

            // var checkResult = await CheckVideoGenerationApproveRules(placed.CustomerId, placed.CustomerId, placed.ReleaseTime);
            // if (checkResult.Key)
            // {
            //     await _appContentRepository.SetVideoGenerationApprovedAsync(id: appContentId);
            //
            //     _logger.FrameworkInfoLog(LogHelper.Generate(
            //         message: "AppContent video generation approved",
            //         reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.NormalizedRequestId },
            //         facility: AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED,
            //         correlationId: correlationId,
            //         exception: null
            //     ));
            //
            //     // Add video generation history for client quote control
            //     await _clientVideoGenerationHistoryRepository.CreateAsync(tenantId: placed.CustomerId, customerId: placed.CustomerId,
            //         videoGenerationDate: placed.ReleaseTime?.Date ?? DateTime.UtcNow.Date,
            //         videoGenerationType: VideoGenerationTypes.DirectVideoGeneration,
            //         contentReferenceIds: placed.Id.ToString());
            //
            //     // Integration Event for VideoGeneratorService
            //     await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            //         eventMessage: new VideoGenerationApprovedEto(
            //             TenantId: placed.TenantId,
            //             ClientId: placed.ClientId,
            //             ReferenceContentType: ReferenceContentTypes.APP_REQUEST_CONTENT,
            //             ReferenceContentId: placed.Id
            //         ));
            // }
            // else
            // {
            //     await _appContentRepository.SetVideoGenerationRejectedAsync(appContentId, checkResult.Value);
            //
            //     _logger.FrameworkInfoLog(LogHelper.Generate(
            //         message: "AppContent video generation rejected",
            //         reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.NormalizedRequestId },
            //         facility: checkResult.Value,
            //         correlationId: correlationId,
            //         exception: null
            //     ));
            // }
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "AppContent normalized fail",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.NormalizedRequestId },
                facility: AppContentOperationFacilities.APP_CONTENT_NORMALIZED_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetVideoGenerationRequestReferenceAsync(Guid appContentId, Guid videoRequestId)
    {
        if (appContentId == Guid.Empty || videoRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _appContentRepository.SetVideoRequestReferenceAsync(id: appContentId, videoRequestId: videoRequestId);
    }

    public async Task SetVideoGenerationResultAsync(Guid appContentId, Guid videoRequestId, bool isGenerateSuccess, string storageVideoUrl = null, string correlationId = null)
    {
        if (appContentId == Guid.Empty || videoRequestId == Guid.Empty || (isGenerateSuccess && string.IsNullOrWhiteSpace(storageVideoUrl)))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _appContentRepository.SetVideoGenerationResultAsync(
            id: appContentId,
            isGenerateSuccess: isGenerateSuccess,
            videoRequestId: videoRequestId,
            storageVideoUrl: storageVideoUrl);

        if (placed.OperationStatus == AppContentOperationStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: "Video generation success",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_SUCCESS,
                correlationId: correlationId,
                exception: null
            ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: "Video generation fail",
                reference: new { placed.ScopeKey, RefContentId = placed.Id, placed.VideoRequestId },
                facility: AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_FAIL,
                correlationId: correlationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid appContentId, string failedReason, string correlationId = null)
    {
        if (appContentId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _appContentRepository.SetStatusToFailedAsync(id: appContentId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"AppContent status fail: {failedReason ?? string.Empty}",
            reference: new { placed.ScopeKey, RefContentId = placed.Id },
            facility: AppContentOperationFacilities.APP_CONTENT_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }

    public async Task TrendVideoGenerationQueryAsync(TrendVideoGenerationQueryEto input, string correlationId = null)
    {
        if (input?.AppClientId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var client = await _customerContentSettingRepository.GetSingleOrDefaultAsync(x => x.Id == input.AppClientId);
        if (client == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "BEGIN");

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < client.DailyTrendVideoGenerationStartedUtcHour)
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}",
                client.DomainName, "SKIPPED", AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "END");
            return;
        }

        // check client trend video generation quote available
        long clientDailyTrendVideoHistoryCount = await _clientVideoGenerationHistoryRepository.GetCustomerVideoHistoryCountAsync(client.Id,
            DateTime.UtcNow.Date, VideoGenerationTypes.TrendVideoGeneration);

        long clientDailyTrendVideoGenerationLimit = client.DailyTrendVideoGenerationLimit - clientDailyTrendVideoHistoryCount;
        var clientQuoteResult = clientDailyTrendVideoGenerationLimit <= 0
            ? new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED);
        if (clientQuoteResult.Key)
        {
            var contentIds = await _appContentRepository.GetCustomerDailyTrendContentIdsAsync
            (
                customerId: client.CustomerId,
                dailyTrendVideoWaitStatisticHour: client.DailyTrendVideoWaitStatisticHour
            );
            if (contentIds is { Count: > 0 })
            {
                var contentVisitList = await _appContentVisitRepository.GetContentIdsVisitCountsAsync(contentIds: contentIds,
                    isMaxCountOrdered: true,
                    contentOrderedLimit: clientDailyTrendVideoGenerationLimit,
                    contentVisitedCountLimit: client.DailyTrendVideoMinVisitCount);

                if (contentVisitList is { Count: > 0 })
                {
                    foreach (var contentVisit in contentVisitList)
                    {
                        var placed = await _appContentRepository.GetSingleOrDefaultAsync(x => x.Id == contentVisit.AppContentId);
                        if (placed is { OperationStatus: AppContentOperationStates.VideoGenerationRejectedReturnAnalysisVideo })
                        {
                            bool operationSuccess;
                            string errorMessage = string.Empty;
                            try
                            {
                                await _appContentRepository.SetVideoGenerationApprovedAsync(id: contentVisit.AppContentId);

                                _logger.FrameworkInfoLog(LogHelper.Generate(
                                    message: "Trend content video generation approved",
                                    reference: new { placed.ScopeKey, AppContentId = placed.Id, placed.NormalizedRequestId },
                                    facility: AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED,
                                    correlationId: placed.CorrelationId,
                                    exception: null
                                ));

                                // // Add video generation history for client quote control
                                // await _clientVideoGenerationHistoryRepository.CreateAsync(tenantId: placed.TenantId, customerId: placed.ClientId,
                                //     videoGenerationDate: placed.ReleaseTime?.Date ?? DateTime.UtcNow.Date,
                                //     videoGenerationType: VideoGenerationTypes.TrendVideoGeneration,
                                //     contentReferenceIds: placed.Id.ToString());
                                //
                                // // Integration Event for TextNormalizerGeneratorService
                                // await EventBus.PublishAsync(correlationId: placed.CorrelationId,
                                //     eventMessage: new VideoGenerationApprovedEto(
                                //         TenantId: placed.TenantId,
                                //         ClientId: placed.ClientId,
                                //         ReferenceContentType: ReferenceContentTypes.APP_REQUEST_CONTENT,
                                //         ReferenceContentId: placed.Id
                                //     ));

                                operationSuccess = true;
                            }
                            catch (Exception e)
                            {
                                operationSuccess = false;
                                errorMessage = e.Message;
                            }

                            if (operationSuccess)
                            {
                                _logger.LogInformation("Client[{ClientDomain}] | CONTENT: {AppContentId}, VISIT: {VisitCount}", client.DomainName,
                                    contentVisit.AppContentId.ToString(), contentVisit.VisitCount.ToString());
                            }
                            else
                            {
                                _logger.LogError("Client[{ClientDomain}] | CONTENT: {AppContentId}, {OperationStatus} | {FailReason}", client.DomainName,
                                    contentVisit.AppContentId.ToString(), "FAIL", errorMessage);

                                _logger.FrameworkErrorLog(LogHelper.Generate(
                                    message: $"Trend content video generation rejected: {errorMessage}",
                                    reference: new { placed.ScopeKey, AppContentId = placed.Id, placed.NormalizedRequestId },
                                    facility: AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED,
                                    correlationId: placed.CorrelationId,
                                    exception: null
                                ));

                                await _appContentRepository.SetVideoGenerationRejectedAsync(id: contentVisit.AppContentId, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_REGENERATION_FAILED);
                            }
                        }
                        else
                        {
                            _logger.LogError("Client[{ClientDomain}] | CONTENT: {AppContentId}, {OperationStatus} | {FailReason}", client.DomainName,
                                contentVisit.AppContentId.ToString(), "FAIL", "CONTENT_INFO_NOT_FOUND");
                        }

                        // Loop period
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", "NOT_ENOUGH_VISIT_FOR_DAILY_TREND");
                }
            }
            else
            {
                _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", "NO_DAILY_TREND_CONTENT");
            }
        }
        else
        {
            _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", clientQuoteResult.Value);
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "END");
    }

    public async Task TestQueryRequestedAsync(TestQueryRequestedEto input, string correlationId = null)
    {
        await Task.Delay(10000);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new TestQueryCompletedEto(
                AppClientId: input.AppClientId
            ));
    }

    private async Task<KeyValuePair<bool, string>> CheckVideoGenerationApproveRules(Guid tenantId, Guid customerContentSettingId, DateTime? releaseTime)
    {
        if (_serviceSettings.SkipContentCheckOperation)
        {
            return new KeyValuePair<bool, string>(true, AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED);
        }

        // Check content release time
        if (!releaseTime.HasValue || releaseTime.Value.ToUniversalTime().Date != DateTime.UtcNow.Date)
        {
            return new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_OLD_CONTENT);
        }

        // Get Client Details
        var client = await _customerContentSettingRepository.GetSingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == customerContentSettingId);
        if (client == null)
        {
            throw new CustomerContentSettingNotFoundException(L, customerContentSettingId.ToString());
        }

        // Check client video generation started settings
        if (DateTime.UtcNow.Hour < client.DailyDirectVideoGenerationStartedUtcHour)
        {
            if (DateTime.UtcNow.Hour < releaseTime.Value.ToUniversalTime().Hour)
            {
                return new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_EARLY_TIME);
            }
        }

        // Check client direct video generation limit
        if (client.DailyDirectVideoGenerationLimit <= 0)
        {
            return new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_DAILY_LIMIT);
        }

        // Check client direct video generation available
        long clientDailyDirectVideoHistoryCount = await _clientVideoGenerationHistoryRepository.GetCustomerVideoHistoryCountAsync(customerContentSettingId,
            releaseTime.Value.ToUniversalTime().Date, VideoGenerationTypes.DirectVideoGeneration);

        return client.DailyDirectVideoGenerationLimit - clientDailyDirectVideoHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, AppContentOperationFacilities.VIDEO_GENERATION_SKIPPED_DAILY_LIMIT)
            : new KeyValuePair<bool, string>(true, AppContentOperationFacilities.APP_CONTENT_VIDEO_GENERATION_APPROVED);
    }
}