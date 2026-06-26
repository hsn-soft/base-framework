using System.Globalization;
using System.Net;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Submits;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.SettingDomain.Dtos;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Exceptions;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class CustomerContentPublicAppService(
    IServiceProvider provider,
    ICustomerContentRepository customerContentRepository,
    ICustomerContentVisitRepository customerContentVisitRepository,
    IAnalysisContentRepository analysisContentRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    ITraceAccesor traceAccessor,
    IDataFilter dataFilter
) : ApplicationServiceBase(provider), ICustomerContentPublicAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task<GetOrCreateCustomerContentResponseDto> GetOrCreateAsync(GetOrCreateCustomerContentRequestDto input, CancellationToken cancellationToken = default)
    {
        if (input.CustomerId == null || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.ContentKey))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE START", input.DomainName, input.ContentKey);

        // normalize properties for queries
        input.DomainName = StringHelper.Minimize(input.DomainName);
        input.ContentKey = StringHelper.Minimize(input.ContentKey);
        string scopeKey = ScopeKeyHelper.Generate(input.CustomerId.Value, ProductTypes.VideoPlatform);

        // Check path is root
        if (input.ContentKey.Equals("/"))
        {
            _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}",
                input.DomainName,
                input.ContentKey,
                PublicContentStatus.SKIPPED_PATH);

            // Add statistic record
            await customerContentVisitRepository.CreateAsync(
                scopeKey: scopeKey,
                customerContentId: Guid.Empty,
                visitResponse: PublicContentStatus.SKIPPED_PATH
            );

            return new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.SKIPPED_CONTENT, ContentStatus = PublicContentStatus.SKIPPED_PATH };
        }

        // Check Client
        bool customerVpSettingCheckResult = false;
        using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
        {
            customerVpSettingCheckResult = await customerVpSettingRepository.ExistsAsync(x =>
                    x.ScopeKey == scopeKey
                    && x.IsBlocked == false
                    && (x.DomainName == input.DomainName || x.DomainName == "www." + input.DomainName)
                , cancellationToken);
        }

        if (!customerVpSettingCheckResult)
        {
            throw new CustomerVpSettingInvalidDomainException(L, input.CustomerId.ToString())
                .WithData(nameof(input.DomainName), input.DomainName);
        }

        string normalizedSlugKey = StringHelper.SlugKeyNormalize(input.ContentKey);
        CustomerContentStatusDto customerContentStatusModel = null;
        using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
        {
            customerContentStatusModel = await customerContentRepository.GetFirstOrDefaultAsync<CustomerContentStatusDto>(
                x => x.ScopeKey == scopeKey && x.SlugKey.Equals(normalizedSlugKey),
                Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        }

        GetOrCreateCustomerContentResponseDto result;
        if (customerContentStatusModel != null)
        {
            result = customerContentStatusModel.OperationStatus switch
            {
                CustomerContentOperationStates.OperationFail => new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.CUSTOMER_CONTENT, ContentId = customerContentStatusModel.CustomerContentId, ContentStatus = PublicContentStatus.FAILED },
                CustomerContentOperationStates.OperationSuccess => new GetOrCreateCustomerContentResponseDto
                {
                    ContentType = PublicContentType.CUSTOMER_CONTENT, ContentId = customerContentStatusModel.CustomerContentId, ContentStatus = PublicContentStatus.READY, ContentVideoUrl = customerContentStatusModel.StorageVideoUrl
                },
                _ => new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.CUSTOMER_CONTENT, ContentId = customerContentStatusModel.CustomerContentId, ContentStatus = PublicContentStatus.PROCESSING }
            };

            if (customerContentStatusModel.OperationStatus != CustomerContentOperationStates.OperationSuccess)
            {
                result = new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.ANALYSIS_CONTENT, ContentStatus = PublicContentStatus.NO_ANALYSIS_VIDEO };

                using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
                {
                    var analysisEndDate = DateTime.UtcNow.Date.AddDays(1);
                    // TODO: analysisVideoUrl = RANDOM(1)
                    var analysisContent = await analysisContentRepository.GetFirstOrDefaultAsync(
                        x => x.ScopeKey == scopeKey
                             && x.VideoStatus == StatusNames.Completed
                             && x.AnalysisDate < analysisEndDate,
                        selector: s => new { s.Id, s.VideoCdnUrl },
                        orderByEntity: o => o.OrderByDescending(x => x.CreationTime),
                        cancellationToken: cancellationToken);

                    if (analysisContent != null)
                    {
                        result = new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.ANALYSIS_CONTENT, ContentId = analysisContent.Id, ContentStatus = PublicContentStatus.READY, ContentVideoUrl = analysisContent.VideoCdnUrl };
                    }
                    else
                    {
                        analysisEndDate = DateTime.UtcNow.Date;
                        // TODO: analysisVideoUrl = RANDOM(1)
                        analysisContent = await analysisContentRepository.GetFirstOrDefaultAsync(
                            x => x.ScopeKey == scopeKey
                                 && x.VideoStatus == StatusNames.Completed
                                 && x.AnalysisDate < analysisEndDate,
                            selector: s => new { s.Id, s.VideoCdnUrl },
                            orderByEntity: o => o.OrderByDescending(x => x.AnalysisDate),
                            cancellationToken: cancellationToken);

                        if (analysisContent != null)
                        {
                            result = new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.ANALYSIS_CONTENT, ContentId = analysisContent.Id, ContentStatus = PublicContentStatus.READY, ContentVideoUrl = analysisContent.VideoCdnUrl };
                        }
                    }
                }
            }

            // Add statistic record
            await customerContentVisitRepository.CreateAsync(
                scopeKey: scopeKey,
                customerContentId: customerContentStatusModel.CustomerContentId,
                visitResponse: result.ContentStatus
            );
        }
        else
        {
            // Get Client Details
            CustomerVpSettingCheckDto customerVpSettingCheck = null;
            using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
            {
                customerVpSettingCheck = await customerVpSettingRepository.GetSingleOrDefaultAsync<CustomerVpSettingCheckDto>(
                    predicate: x => x.ScopeKey == scopeKey,
                    configuration: Mapper.ConfigurationProvider,
                    cancellationToken: cancellationToken);
            }

            if (customerVpSettingCheck == null)
            {
                throw new CustomerVpSettingNotFoundException(L, input.CustomerId.ToString());
            }

            // Check Client IncludePathFilter exist
            if (customerVpSettingCheck.IncludePathFilters is { Count: > 0 })
            {
                bool isFilterSuccess = false;
                var currentPathFilterArray = input.ContentKey.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (currentPathFilterArray is { Count: > 0 })
                {
                    string currentPathText = "/" + string.Join("/", currentPathFilterArray);
                    if (customerVpSettingCheck.IncludePathFilters
                        .Select(filter => filter.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList())
                        .Select(filterArray => "/" + string.Join("/", filterArray))
                        .Any(filterText => currentPathText.StartsWith(filterText + "/"))) //TODO: sondakika com için (filterText+"/").lenght + 1 olacak
                    {
                        isFilterSuccess = true;
                    }
                }

                if (!isFilterSuccess)
                {
                    _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}",
                        input.DomainName,
                        input.ContentKey,
                        PublicContentStatus.SKIPPED_PATH);

                    // Add statistic record
                    await customerContentVisitRepository.CreateAsync(
                        scopeKey: scopeKey,
                        customerContentId: Guid.Empty,
                        visitResponse: PublicContentStatus.SKIPPED_PATH
                    );

                    return new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.SKIPPED_CONTENT, ContentStatus = PublicContentStatus.SKIPPED_PATH };
                }
            }

            // Check Client ExcludePathFilter exist
            if (customerVpSettingCheck.ExcludePathFilters is { Count: > 0 })
            {
                var currentPathFilterArray = input.ContentKey.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (currentPathFilterArray is { Count: > 0 })
                {
                    string currentPathText = "/" + string.Join("/", currentPathFilterArray);
                    if (customerVpSettingCheck.ExcludePathFilters
                        .Select(filter => filter.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList())
                        .Select(filterArray => "/" + string.Join("/", filterArray))
                        .Any(filterText => currentPathText.StartsWith(filterText + "/")))
                    {
                        _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}", input.DomainName, input.ContentKey, PublicContentStatus.SKIPPED_PATH);

                        // Add statistic record
                        await customerContentVisitRepository.CreateAsync(
                            scopeKey: scopeKey,
                            customerContentId: Guid.Empty,
                            visitResponse: PublicContentStatus.SKIPPED_PATH
                        );

                        return new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.SKIPPED_CONTENT, ContentStatus = PublicContentStatus.SKIPPED_PATH };
                    }
                }
            }

            // Add appContent record
            var placedCustomerContent = await customerContentRepository.CreateAsync(
                scopeKey: ScopeKeyHelper.Generate(input.CustomerId.Value, ProductTypes.VideoPlatform),
                contentKey: input.ContentKey,
                correlationId: traceAccessor?.GetCorrelationId());

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"CustomerContent created {input.ContentKey}",
                reference: new { placedCustomerContent.ScopeKey, ClientDomain = customerVpSettingCheck.DomainName, input.ContentKey, RefContentId = placedCustomerContent.Id },
                facility: EventNames.CustomerContentCreated,
                correlationId: placedCustomerContent.CorrelationId,
                exception: null
            ));

            result = new GetOrCreateCustomerContentResponseDto { ContentType = PublicContentType.CUSTOMER_CONTENT, ContentId = placedCustomerContent.Id, ContentStatus = PublicContentStatus.CREATED };

            // Add statistic record
            await customerContentVisitRepository.CreateAsync(
                scopeKey: placedCustomerContent.ScopeKey,
                customerContentId: placedCustomerContent.Id,
                visitResponse: result.ContentStatus);

            // Integration Event for TextNormalizerService
            await EventBus.PublishAsync(
                correlationId: placedCustomerContent.CorrelationId,
                eventMessage: new CustomerContentCreatedEto { ScopeKey = placedCustomerContent.ScopeKey, CustomerContentId = placedCustomerContent.Id, DomainName = customerVpSettingCheck.DomainName, DomainPath = input.ContentKey }
            );
        }

        if (result.ContentStatus == PublicContentStatus.READY)
        {
            _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentVideoUrl {ContentVideoUrl}", input.DomainName, input.ContentKey, result.ContentVideoUrl);
        }
        else
        {
            _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}", input.DomainName, input.ContentKey, result.ContentStatus);
        }

        return result;
    }

    public async Task CreateAdResultAsync(CreateContentAdResultDto input, CancellationToken cancellationToken = default)
    {
        if (input.ContentId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.ContentType)
            || string.IsNullOrWhiteSpace(input.FeedKey)
            || input.ContentType is not (PublicContentType.CUSTOMER_CONTENT or PublicContentType.ANALYSIS_CONTENT))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        _logger.LogDebug("ReferenceContentId: {ReferenceContentId}, ReferenceContentType: {ReferenceContentType}, FeedKey: {FeedKey}, FeedMessage: {FeedMessage} CREATE AD RESULT",
            input.ContentId,
            input.ContentType,
            input.FeedKey,
            input.FeedMessage
        );

        if (input.FeedKey is not (AdsFacilities.ADS_ERROR
            or AdsFacilities.ADS_TIMEOUT
            or AdsFacilities.ADS_EMPTY
            or AdsFacilities.ADS_STARTED))
        {
            return;
        }

        switch (input.ContentType)
        {
            case PublicContentType.CUSTOMER_CONTENT:
                {
                    CustomerContent customerContent;
                    using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
                    {
                        customerContent = await customerContentRepository.GetSingleOrDefaultAsync(x
                            => x.Id == input.ContentId, cancellationToken: cancellationToken);
                    }

                    if (customerContent == null)
                    {
                        throw new CustomerContentNotFoundException(L, input.ContentId);
                    }

                    // TODO: Save statistics to event manager
                    break;
                }
            case PublicContentType.ANALYSIS_CONTENT:
                {
                    AnalysisContent analysisContent;
                    using (dataFilter.Disable<IScopeSubscription>()) // anonymous user , unknown tenant
                    {
                        analysisContent = await analysisContentRepository.GetSingleOrDefaultAsync(x
                            => x.Id == input.ContentId, cancellationToken: cancellationToken);
                    }

                    if (analysisContent == null)
                    {
                        throw new AnalysisContentNotFoundException(L, input.ContentId.ToString());
                    }

                    // TODO: Save statistics to event manager
                    break;
                }
        }
    }
}