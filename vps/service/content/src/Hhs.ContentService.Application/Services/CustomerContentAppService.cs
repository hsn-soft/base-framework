using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.ContentService.Domain.Settings;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
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
            .And(pagedInput.ReleaseTimeStart.HasValue ? e => e.ScrapReleaseTimeUtc != null && e.ScrapReleaseTimeUtc >= pagedInput.ReleaseTimeStart.Value : null)
            .And(pagedInput.ReleaseTimeEnd.HasValue ? e => e.ScrapReleaseTimeUtc != null && e.ScrapReleaseTimeUtc < pagedInput.ReleaseTimeEnd.Value : null)
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
            .And(filterInput.ReleaseTimeStart.HasValue ? e => e.ScrapReleaseTimeUtc != null && e.ScrapReleaseTimeUtc >= filterInput.ReleaseTimeStart.Value : null)
            .And(filterInput.ReleaseTimeEnd.HasValue ? e => e.ScrapReleaseTimeUtc != null && e.ScrapReleaseTimeUtc < filterInput.ReleaseTimeEnd.Value : null)
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
                customerVpSetting.DomainName, "SKIPPED", "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_EARLY_TIME");
            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
            return;
        }

        // check client trend video generation quote available
        long clientDailyTrendVideoHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(customerVpSetting.ScopeKey,
            DateTime.UtcNow.Date, VideoGenerationTypes.TrendVideoGeneration);

        long clientDailyTrendVideoGenerationLimit = customerVpSetting.DailyTrendVideoGenerationLimit - clientDailyTrendVideoHistoryCount;
        var clientQuoteResult = clientDailyTrendVideoGenerationLimit <= 0
            ? new KeyValuePair<bool, string>(false, "CUSTOMER_CONTENT_VIDEO_GENERATION_SKIPPED_DAILY_LIMIT")
            : new KeyValuePair<bool, string>(true, "CUSTOMER_CONTENT_VIDEO_GENERATION_APPROVED");
        if (clientQuoteResult.Key)
        {
            var contentIds = await customerContentRepository.GetCustomerDailyTrendContentIdsAsync
            (
                scopeKey: customerVpSetting.ScopeKey,
                dailyTrendVideoWaitStatisticHour: customerVpSetting.DailyTrendContentWaitStatisticHour
            );
            if (contentIds is { Count: > 0 })
            {
                var contentVisitList = await customerContentVisitRepository.GetContentIdsVisitCountsAsync(customerContentIds: contentIds,
                    isMaxCountOrdered: true,
                    customerContentOrderedLimit: clientDailyTrendVideoGenerationLimit,
                    customerContentVisitedCountLimit: customerVpSetting.DailyTrendContentMinVisitCount);

                if (contentVisitList is { Count: > 0 })
                {
                    _logger.LogWarning(
                        "Client[{ClientDomain}] | {OperationStatus} => {QueryResult} | Pending items: {Count}",
                        customerVpSetting.DomainName, "SKIPPED", "TREND_VIDEO_GENERATION_NOT_IMPLEMENTED", contentVisitList.Count);
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
}