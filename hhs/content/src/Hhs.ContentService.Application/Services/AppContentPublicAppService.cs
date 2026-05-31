using System.Globalization;
using System.Net;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Domain.ClientDomain.Exceptions;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Tracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class AppContentPublicAppService(
    IServiceProvider provider,
    IAppContentRepository appContentRepository,
    IAppContentVisitRepository contentVisitRepository,
    IAnalysisContentRepository analysisContentRepository,
    IClientRepository clientRepository,
    ITraceAccesor traceAccessor,
    IDataFilter dataFilter
) : ApplicationServiceBase(provider), IAppContentPublicAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task<GetOrCreateAppContentResponseDto> GetOrCreateAsync(GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default)
    {
        if (input.CustomerId == null || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.ContentKey))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE START", input.DomainName, input.ContentKey);

        // normalize properties for queries
        input.DomainName = StringOperations.Minimize(input.DomainName);
        input.ContentKey = StringOperations.Minimize(input.ContentKey);

        // Check path is root
        if (input.ContentKey.Equals("/"))
        {
            _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}", input.DomainName, input.ContentKey, AppContentPublicStatus.SKIPPED_PATH);

            // Add statistic record
            await contentVisitRepository.CreateAsync(clientId: input.CustomerId.Value, appContentId: Guid.Empty, visitResponse: AppContentPublicStatus.SKIPPED_PATH);

            return new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.SKIPPED_CONTENT, ContentStatus = AppContentPublicStatus.SKIPPED_PATH };
        }

        // Check Client
        bool clientCheckResult = false;
        using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            clientCheckResult = await clientRepository.ExistsAsync(x =>
                    x.Id == input.CustomerId.Value
                    && x.IsBlocked == false
                    && (x.DomainName == input.DomainName || x.DomainName == "www." + input.DomainName)
                , cancellationToken);
        }

        if (!clientCheckResult)
        {
            throw new ClientInvalidDomainException(L, input.CustomerId.ToString())
                .WithData(nameof(input.DomainName), input.DomainName);
        }

        string normalizedSlugKey = StringOperations.SlugKeyNormalize(input.ContentKey);
        AppContentStatusDto contentStatusModel = null;
        using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            contentStatusModel = await appContentRepository.GetFirstOrDefaultAsync<AppContentStatusDto>(
                x => x.ClientId == input.CustomerId.Value && x.SlugKey.Equals(normalizedSlugKey),
                Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        }

        GetOrCreateAppContentResponseDto result;
        if (contentStatusModel != null)
        {
            result = contentStatusModel.OperationStatus switch
            {
                AppContentOperationStates.OperationFail => new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.APP_CONTENT, ContentId = contentStatusModel.AppContentId, ContentStatus = AppContentPublicStatus.FAILED },
                AppContentOperationStates.OperationSuccess => new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.APP_CONTENT, ContentId = contentStatusModel.AppContentId, ContentStatus = AppContentPublicStatus.READY, ContentVideoUrl = contentStatusModel.StorageVideoUrl },
                _ => new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.APP_CONTENT, ContentId = contentStatusModel.AppContentId, ContentStatus = AppContentPublicStatus.PROCESSING }
            };

            if (contentStatusModel.OperationStatus != AppContentOperationStates.OperationSuccess)
            {
                result = new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.ANALYSIS_CONTENT, ContentStatus = AppContentPublicStatus.NO_ANALYSIS_VIDEO };

                using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
                {
                    var analysisEndDate = DateTime.UtcNow.Date.AddDays(1);
                    // TODO: analysisVideoUrl = RANDOM(1)
                    var analysisContent = await analysisContentRepository.GetFirstOrDefaultAsync(
                        x => x.ClientId == input.CustomerId.Value
                             && x.OperationStatus == AnalysisContentOperationStates.OperationSuccess
                             && x.AnalysisDate < analysisEndDate,
                        selector: s => new { s.Id, s.StorageVideoUrl },
                        orderByEntity: o => o.OrderByDescending(x => x.CreationTime),
                        cancellationToken: cancellationToken);

                    if (analysisContent != null)
                    {
                        result = new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.ANALYSIS_CONTENT, ContentId = analysisContent.Id, ContentStatus = AppContentPublicStatus.READY, ContentVideoUrl = analysisContent.StorageVideoUrl };
                    }
                    else
                    {
                        analysisEndDate = DateTime.UtcNow.Date;
                        // TODO: analysisVideoUrl = RANDOM(1)
                        analysisContent = await analysisContentRepository.GetFirstOrDefaultAsync(
                            x => x.ClientId == input.CustomerId.Value
                                 && x.OperationStatus == AnalysisContentOperationStates.OperationSuccess
                                 && x.AnalysisDate < analysisEndDate,
                            selector: s => new { s.Id, s.StorageVideoUrl },
                            orderByEntity: o => o.OrderByDescending(x => x.AnalysisDate),
                            cancellationToken: cancellationToken);

                        if (analysisContent != null)
                        {
                            result = new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.ANALYSIS_CONTENT, ContentId = analysisContent.Id, ContentStatus = AppContentPublicStatus.READY, ContentVideoUrl = analysisContent.StorageVideoUrl };
                        }
                    }
                }
            }

            // Add statistic record
            await contentVisitRepository.CreateAsync(clientId: input.CustomerId.Value, appContentId: contentStatusModel.AppContentId, visitResponse: result.ContentStatus);
        }
        else
        {
            // Get Client Details
            ClientCheckDto clientCheck = null;
            using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
            {
                clientCheck = await clientRepository.GetSingleOrDefaultAsync<ClientCheckDto>(
                    predicate: x => x.Id == input.CustomerId.Value,
                    includeEntity: q => q.Include(x => x.PathFilters),
                    configuration: Mapper.ConfigurationProvider,
                    cancellationToken: cancellationToken);
            }

            if (clientCheck == null)
            {
                throw new ClientNotFoundException(L, input.CustomerId.ToString());
            }

            // Check Client IncludePathFilter exist
            if (clientCheck.PathFilters.Where(x => x.Key == ClientFilterTypes.IncludeFilter).ToList() is { Count: > 0 })
            {
                bool isFilterSuccess = false;
                var currentPathFilterArray = input.ContentKey.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (currentPathFilterArray is { Count: > 0 })
                {
                    string currentPathText = "/" + string.Join("/", currentPathFilterArray);
                    var includeFilterList = clientCheck.PathFilters.Where(x => x.Key == ClientFilterTypes.IncludeFilter).Select(x => x.Value).ToList();
                    if (includeFilterList
                        .Select(filter => filter.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList())
                        .Select(filterArray => "/" + string.Join("/", filterArray))
                        .Any(filterText => currentPathText.StartsWith(filterText + "/"))) //TODO: sondakika com için (filterText+"/").lenght + 1 olacak
                    {
                        isFilterSuccess = true;
                    }
                }

                if (!isFilterSuccess)
                {
                    _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}", input.DomainName, input.ContentKey, AppContentPublicStatus.SKIPPED_PATH);

                    // Add statistic record
                    await contentVisitRepository.CreateAsync(clientId: input.CustomerId.Value, appContentId: Guid.Empty, visitResponse: AppContentPublicStatus.SKIPPED_PATH);

                    return new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.SKIPPED_CONTENT, ContentStatus = AppContentPublicStatus.SKIPPED_PATH };
                }
            }

            // Check Client ExcludePathFilter exist
            if (clientCheck.PathFilters.Where(x => x.Key == ClientFilterTypes.ExcludeFilter).ToList() is { Count: > 0 })
            {
                var currentPathFilterArray = input.ContentKey.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (currentPathFilterArray is { Count: > 0 })
                {
                    string currentPathText = "/" + string.Join("/", currentPathFilterArray);
                    var excludeFilterList = clientCheck.PathFilters.Where(x => x.Key == ClientFilterTypes.ExcludeFilter).Select(x => x.Value).ToList();
                    if (excludeFilterList
                        .Select(filter => filter.ToLower(new CultureInfo("en-US")).Split("/").Where(x => !string.IsNullOrWhiteSpace(x)).ToList())
                        .Select(filterArray => "/" + string.Join("/", filterArray))
                        .Any(filterText => currentPathText.StartsWith(filterText + "/")))
                    {
                        _logger.LogDebug("DomainName: {DomainName}, ContentKey: {ContentKey} GET OR CREATE FINISHED ContentStatus {ContentStatus}", input.DomainName, input.ContentKey, AppContentPublicStatus.SKIPPED_PATH);

                        // Add statistic record
                        await contentVisitRepository.CreateAsync(clientId: input.CustomerId.Value, appContentId: Guid.Empty, visitResponse: AppContentPublicStatus.SKIPPED_PATH);

                        return new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.SKIPPED_CONTENT, ContentStatus = AppContentPublicStatus.SKIPPED_PATH };
                    }
                }
            }

            // Add appContent record
            var placed = await appContentRepository.CreateAsync(
                tenantId: clientCheck.TenantId,
                clientId: clientCheck.Id,
                slugKey: normalizedSlugKey,
                operationStatus: AppContentOperationStates.CreatedWaitForNormalize,
                correlationId: traceAccessor?.GetCorrelationId());

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"AppContent created {input.ContentKey}",
                reference: new
                {
                    placed.TenantId,
                    placed.ClientId,
                    ClientDomain = clientCheck.DomainName,
                    input.ContentKey,
                    RefContentId = placed.Id
                },
                facility: AppContentOperationFacilities.APP_CONTENT_CREATED,
                correlationId: placed.CorrelationId,
                exception: null
            ));

            result = new GetOrCreateAppContentResponseDto { ContentType = AppContentPublicType.APP_CONTENT, ContentId = placed.Id, ContentStatus = AppContentPublicStatus.CREATED };

            // Add statistic record
            await contentVisitRepository.CreateAsync(clientId: placed.ClientId, appContentId: placed.Id, visitResponse: result.ContentStatus);

            // Integration Event for TextNormalizerService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AppContentNormalizedStartedEto(
                    TenantId: placed.TenantId,
                    ClientId: placed.ClientId,
                    AppContentId: placed.Id,
                    DomainName: clientCheck.DomainName,
                    DomainPath: input.ContentKey
                ));
        }

        if (result.ContentStatus == AppContentPublicStatus.READY)
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
        if (input.CustomerId == Guid.Empty
            || input.ContentId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.ContentType)
            || string.IsNullOrWhiteSpace(input.FeedKey)
            || input.ContentType is not (AppContentPublicType.APP_CONTENT or AppContentPublicType.ANALYSIS_CONTENT))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        _logger.LogDebug("CustomerId: {CustomerId}, ContentId: {ContentId}, FeedKey: {FeedKey}, FeedMessage: {FeedMessage}, Message: {Message} CREATE AD RESULT"
            , input.CustomerId, input.ContentId, input.ContentType, input.FeedKey, input.FeedMessage);

        if (input.FeedKey is not (AppContentOperationFacilities.ADS_ERROR
            or AppContentOperationFacilities.ADS_TIMEOUT
            or AppContentOperationFacilities.ADS_EMPTY
            or AppContentOperationFacilities.ADS_STARTED))
        {
            return;
        }

        string clientDomain;
        using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            clientDomain = await clientRepository.GetSingleOrDefaultAsync(x => x.Id == input.CustomerId && x.IsBlocked == false, selector: s => s.DomainName, cancellationToken: cancellationToken);
        }

        if (clientDomain == null)
        {
            throw new ClientInvalidDomainException(L, input.CustomerId.ToString())
                .WithData(nameof(input.CustomerId), input.CustomerId);
        }

        switch (input.ContentType)
        {
            case AppContentPublicType.APP_CONTENT:
                {
                    AppContent content;
                    using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
                    {
                        content = await appContentRepository.GetSingleOrDefaultAsync(x => x.Id == input.ContentId, cancellationToken: cancellationToken);
                    }

                    if (content == null)
                    {
                        throw new AppContentNotFoundException(L, input.ContentId.ToString())
                            .WithData(nameof(input.ContentId), input.ContentId);
                    }

                    // TODO: Save statistics to event manager
                    break;
                }
            case AppContentPublicType.ANALYSIS_CONTENT:
                {
                    AnalysisContent analysisContent;
                    using (dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
                    {
                        analysisContent = await analysisContentRepository.GetSingleOrDefaultAsync(x => x.Id == input.ContentId, cancellationToken: cancellationToken);
                    }

                    if (analysisContent == null)
                    {
                        throw new AppContentNotFoundException(L, input.ContentId.ToString())
                            .WithData(nameof(input.ContentId), input.ContentId);
                    }

                    // TODO: Save statistics to event manager
                    break;
                }
        }
    }
}