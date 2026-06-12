using System.Net;
using Hhs.Shared.Contracts.Events.Content;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAIResponses;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.CustomerDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.Domain.Settings;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization.Permissions;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizedRequestAppService : ApplicationServiceBase, INormalizedRequestAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly INormalizedRequestRepository _normalizedRequestRepository;
    private readonly IOutlineProvider _outlineProvider;
    private readonly IScrapingProvider _scrapingProvider;
    private readonly TextNormalizerSettings _serviceSettings;
    private readonly ICustomerConfigurationRepository _customerSettingsRepository;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IPermissionConstraintChecker _permissionConstraintChecker;

    public NormalizedRequestAppService(IServiceProvider provider,
        IOptions<TextNormalizerSettings> serviceSettings,
        INormalizedRequestRepository normalizedRequestRepository,
        IOutlineProvider outlineProvider,
        IScrapingProvider scrapingProvider,
        ICustomerConfigurationRepository customerConfigurationRepository, IPermissionChecker permissionChecker, IPermissionConstraintChecker permissionConstraintChecker) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
        _customerSettingsRepository = customerConfigurationRepository;
        _permissionChecker = permissionChecker;
        _permissionConstraintChecker = permissionConstraintChecker;
        _normalizedRequestRepository = normalizedRequestRepository;
        _outlineProvider = outlineProvider;
        _scrapingProvider = scrapingProvider;
    }

    public async Task<NormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _normalizedRequestRepository.GetSingleOrDefaultAsync<NormalizedRequestDto>(
            predicate: x => x.Id == id && x.IsDeleted == false,
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<NormalizedRequestDto>> GetPagedListAsync(GetNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default)
    {

        // // dependency permission sample
        // if (!await _permissionChecker.IsGrantedAsync("service.invoice.read"))
        // {
        //     throw new UnauthorizedAccessException("service.invoice.read");
        // }
        //
        // // data permission sample
        // if (await _permissionChecker.IsGrantedAsync("data.invoice-report.price.view"))
        // {
        //     dto.Price = entity.Price;
        // }
        // else
        // {
        //     dto.Price = null;
        // }

        // // contraint permission sample
        // int maxDays = await _permissionConstraintChecker.GetIntAsync("constraint.invoice-report.fiscal.max-days") ?? 0;

        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.SearchText = StringHelper.Minimize(pagedInput.SearchText);
        pagedInput.DomainName = StringHelper.Minimize(pagedInput.DomainName);
        pagedInput.DomainPath = StringHelper.Minimize(pagedInput.DomainPath);

        if (pagedInput.CreationTimeEnd.HasValue)
        {
            pagedInput.CreationTimeEnd = pagedInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (pagedInput.ReleaseTimeEnd.HasValue)
        {
            pagedInput.ReleaseTimeEnd = pagedInput.ReleaseTimeEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<NormalizedRequest>()
            .And(e => e.IsDeleted == false)
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.DomainPath.Contains(pagedInput.SearchText) : null)
            .And(pagedInput.ClientId.HasValue ? e => e.ClientId == pagedInput.ClientId.Value : null)
            .And(pagedInput.AppContentId.HasValue ? e => e.AppContentId == pagedInput.AppContentId.Value : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainName) ? e => e.DomainName == pagedInput.DomainName : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainPath) ? e => e.DomainPath.Contains(pagedInput.DomainPath) : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.ReleaseTimeStart.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime >= pagedInput.ReleaseTimeStart.Value : null)
            .And(pagedInput.ReleaseTimeEnd.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime < pagedInput.ReleaseTimeEnd.Value : null)
            .Build();

        var result = await _normalizedRequestRepository.GetPageListAsync<NormalizedRequestDto>(
            options: new PagedQueryOptions<NormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? NormalizedRequestConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<NormalizedRequestDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<NormalizedRequestDto>> GetFilterListAsync(GetNormalizedRequestsFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.DomainName = StringHelper.Minimize(filterInput.DomainName);
        filterInput.DomainPath = StringHelper.Minimize(filterInput.DomainPath);

        if (filterInput.CreationTimeEnd.HasValue)
        {
            filterInput.CreationTimeEnd = filterInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (filterInput.ReleaseTimeEnd.HasValue)
        {
            filterInput.ReleaseTimeEnd = filterInput.ReleaseTimeEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<NormalizedRequest>()
            .And(e => e.IsDeleted == false)
            .And(filterInput.ClientId.HasValue ? e => e.ClientId == filterInput.ClientId.Value : null)
            .And(filterInput.AppContentId.HasValue ? e => e.AppContentId == filterInput.AppContentId.Value : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainName) ? e => e.DomainName == filterInput.DomainName : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainPath) ? e => e.DomainPath.Contains(filterInput.DomainPath) : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.ReleaseTimeStart.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime >= filterInput.ReleaseTimeStart.Value : null)
            .And(filterInput.ReleaseTimeEnd.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime < filterInput.ReleaseTimeEnd.Value : null)
            .Build();

        return await _normalizedRequestRepository.GetListAsync<NormalizedRequestDto>(
            options: new ListQueryOptions<NormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? NormalizedRequestConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<NormalizedRequestSearchDto>> GetSearchListAsync(GetNormalizedRequestsSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringHelper.Minimize(searchInput.SearchText);

        var filter = new FilterBuilder<NormalizedRequest>()
            .And(e => e.IsDeleted == false)
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.DomainPath.Contains(searchInput.SearchText) : null)
            .Build();

        return await _normalizedRequestRepository.GetListAsync<NormalizedRequestSearchDto>(
            options: new ListQueryOptions<NormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? NormalizedRequestConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(AppContentNormalizedStartedEto input, string correlationId = null)
    {
        if (input == null || input.TenantId == Guid.Empty || input.ClientId == Guid.Empty || input.AppContentId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.DomainPath))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var contentEntity = await _normalizedRequestRepository.FindByUniqueKeysAsync(input.ClientId, input.AppContentId);
        if (contentEntity != null) return;

        var placed = await _normalizedRequestRepository.CreateAsync(
            tenantId: input.TenantId,
            clientId: input.ClientId,
            domainName: StringHelper.Minimize(input.DomainName),
            appContentId: input.AppContentId,
            domainPath: StringHelper.Minimize(input.DomainPath),
            operationStatus: NormalizedRequestStates.CreatedWaitForScraping,
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Normalized request created",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                ContentKey = placed.DomainPath,
                RefContentId = placed.AppContentId,
                NormalizedRequestId = placed.Id
            },
            facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_CREATED,
            correlationId: correlationId,
            exception: null
        ));

        // Integration Event for ContentService(set reference)
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AppContentNormalizedRequestCreatedEto(
                AppContentId: placed.AppContentId,
                NormalizedRequestId: placed.Id
            ));

        // Integration Event for TextNormalizerService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizedRequestScrapingStartedEto(
                NormalizedRequestId: placed.Id
            ));
    }

    public async Task ScrapingAsync(Guid normalizedRequestId)
    {
        if (normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var normalizedRequestItem = await _normalizedRequestRepository.GetByIdOrDefaultAsync(normalizedRequestId);
        if (normalizedRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool scrapingOperationSuccess;
        string errorMessage = null;
        ScrapingContentDataModel scrapingContentData = null;
        try
        {
            ScrapingResponseDto response;
            var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(normalizedRequestItem.ClientId);
            if (clientSettings is null) throw new ArgumentNullException(nameof(normalizedRequestItem.ClientId));
            if (!_serviceSettings.SkipScrapingOperation && clientSettings.NormalizerSetting.IsScrapingOperationActive)
            {
                response = await _scrapingProvider.ScrapingAsync(new ScrapingRequestDto { DomainKey = normalizedRequestItem.DomainName, Path = normalizedRequestItem.DomainPath });
            }
            else
            {
                response = new ScrapingResponseDto
                {
                    Title = "Title Test",
                    ReleaseTime = DateTime.UtcNow,
                    Spot = "Spot Test",
                    Details = "Test Details",
                    ImageUrl = ""
                    //ModifiedTime = null //TODO: Check null date
                };
            }

            if (response == null) throw new Exception("SCRAPING DATA NOT FOUND");
            if (response.HasError)
            {
                throw new Exception(string.Join(" ", response.Errors));
            }

            scrapingContentData = new ScrapingContentDataModel
            {
                Title = response.Title ?? string.Empty,
                Spot = response.Spot,
                ReleaseTime = response.ReleaseTime,
                Details = response.Details,
                ImageUrl = response.ImageUrl
                //ModifiedTime = response.ModifiedTime
            };
            scrapingOperationSuccess = !string.IsNullOrWhiteSpace(scrapingContentData.Title)
                                       || !string.IsNullOrWhiteSpace(scrapingContentData.Spot)
                                       || !string.IsNullOrWhiteSpace(scrapingContentData.Details);

            if (!scrapingOperationSuccess) throw new Exception("SCRAPING DATA IS EMPTY");
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_SCRAPING_FAIL,
                correlationId: normalizedRequestItem.CorrelationId,
                exception: ex
            ));
            scrapingOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updated = await _normalizedRequestRepository.SetScrapingResultAsync(
            id: normalizedRequestId,
            isScrapingSuccess: scrapingOperationSuccess,
            errorMessage: errorMessage,
            scrapingContentData: scrapingOperationSuccess ? scrapingContentData : null);

        if (updated.OperationStatus == NormalizedRequestStates.ContentScrappedWaitForOutline)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Normalized request scraping success",
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_SCRAPING_SUCCESS,
                correlationId: updated.CorrelationId,
                exception: null
            ));

            // Integration Event for TextNormalizerService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new NormalizedRequestOutlineStartedEto(
                    NormalizedRequestId: updated.Id
                ));
        }
        else
        {
            //  Integration Event for ContentService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AppContentNormalizedResultEto(
                    AppContentId: normalizedRequestItem.AppContentId,
                    IsNormalizedSuccess: normalizedRequestItem.OperationStatus == NormalizedRequestStates.OperationSuccess,
                    NormalizedRequestId: normalizedRequestItem.Id,
                    ReleaseTime: normalizedRequestItem.ScrapingContentData?.ReleaseTime
                ));
        }
    }

    public async Task OutlineAsync(Guid normalizedRequestId)
    {
        if (normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var normalizedRequestItem = await _normalizedRequestRepository.GetByIdOrDefaultAsync(normalizedRequestId);
        if (normalizedRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool outlineOperationSuccess;
        string errorMessage = null;
        string outlineResultData = null;
        try
        {
            if (normalizedRequestItem.ScrapingContentData != null)
            {
                if (normalizedRequestItem.ScrapingContentData.ReleaseTime != null && normalizedRequestItem.ScrapingContentData.ReleaseTime < DateTime.UtcNow.AddDays(-3))
                {
                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: "Normalized request outline skipped",
                        reference: new
                        {
                            normalizedRequestItem.TenantId,
                            normalizedRequestItem.ClientId,
                            ClientDomain = normalizedRequestItem.DomainName,
                            ContentKey = normalizedRequestItem.DomainPath,
                            RefContentId = normalizedRequestItem.AppContentId,
                            NormalizedRequestId = normalizedRequestItem.Id
                        },
                        facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_OUTLINE_SKIPPED,
                        correlationId: normalizedRequestItem.CorrelationId,
                        exception: null
                    ));

                    //  Integration Event for ContentService
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new AppContentNormalizedResultEto(
                            AppContentId: normalizedRequestItem.AppContentId,
                            IsNormalizedSuccess: normalizedRequestItem.OperationStatus == NormalizedRequestStates.OperationSuccess,
                            NormalizedRequestId: normalizedRequestItem.Id,
                            ReleaseTime: normalizedRequestItem.ScrapingContentData?.ReleaseTime
                        ));

                    return;
                }

                var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(normalizedRequestItem.ClientId);
                if (clientSettings is null) throw new ArgumentNullException(nameof(normalizedRequestItem.ClientId));

                string outlineInput = string.Format("{0} {1} {2}",
                    normalizedRequestItem.ScrapingContentData.Title,
                    normalizedRequestItem.ScrapingContentData.Spot ?? string.Empty,
                    normalizedRequestItem.ScrapingContentData.Details ?? string.Empty
                );

                OutlineResponseDto response;
                if (!_serviceSettings.SkipOutlineOperation && clientSettings.NormalizerSetting.IsOutlineOperationActive)
                {
                    var outlineRequest = new OutlineRequestDto { OutlinePrompt = clientSettings.NormalizerSetting.ContentOutlinePrompt, RefContentType = ReferenceContentTypes.APP_REQUEST_CONTENT, RefContentId = normalizedRequestItem.AppContentId, OutlineInput = outlineInput };
                    response = await _outlineProvider.OutlineAsync(outlineRequest, null, _serviceSettings.UseStructuredOutput);
                }
                else
                {
                    response = new OutlineResponseDto { RefContentId = normalizedRequestItem.AppContentId, OutlinedData = outlineInput };
                }

                if (response != null)
                {
                    if (response.HasError)
                    {
                        throw new Exception("OUTLINE RESPONSE ERROR: " + response.ErrorDetails);
                    }

                    outlineResultData = response.OutlinedData;
                    if (!string.IsNullOrWhiteSpace(outlineResultData))
                    {
                        outlineOperationSuccess = true;
                    }
                    else throw new Exception("OUTLINE RESPONSE CONTENT DATA UNKNOWN");
                }
                else throw new Exception("OUTLINE RESPONSE IS NULL");
            }
            else throw new Exception("OUTLINE SCRAPING DATA UNKNOWN");
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_OUTLINE_FAIL,
                correlationId: normalizedRequestItem.CorrelationId,
                exception: ex
            ));
            outlineOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedNormalizedRequest = await _normalizedRequestRepository.SetOutlineResultAsync(
            id: normalizedRequestId,
            isNormalizedSuccess: outlineOperationSuccess,
            errorMessage: errorMessage,
            outlineResult: outlineOperationSuccess ? outlineResultData : null);

        if (updatedNormalizedRequest.OperationStatus == NormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Normalized request outline success",
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_OUTLINE_SUCCESS,
                correlationId: updatedNormalizedRequest.CorrelationId,
                exception: null
            ));
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AppContentNormalizedResultEto(
                AppContentId: updatedNormalizedRequest.AppContentId,
                IsNormalizedSuccess: updatedNormalizedRequest.OperationStatus == NormalizedRequestStates.OperationSuccess,
                NormalizedRequestId: updatedNormalizedRequest.Id,
                ReleaseTime: updatedNormalizedRequest.ScrapingContentData?.ReleaseTime
            ));
    }

    public async Task VideoGenerationApprovedAsync(VideoGenerationApprovedEto input)
    {
        var normalizedRequestItem = await _normalizedRequestRepository.FindByUniqueKeysAsync(input.ClientId, input.ReferenceContentId);
        if (normalizedRequestItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        if (normalizedRequestItem.OperationStatus == NormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Normalized request video creation trigger success",
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.VIDEO_CREATION_TRIGGER_SUCCESS,
                correlationId: normalizedRequestItem.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationStartedEto(
                    TenantId: normalizedRequestItem.TenantId,
                    ClientId: normalizedRequestItem.ClientId,
                    DomainName: normalizedRequestItem.DomainName,
                    ReferenceContentType: ReferenceContentTypes.APP_REQUEST_CONTENT,
                    ReferenceContentId: normalizedRequestItem.AppContentId,
                    EncodedNormalizedContentDatas: new List<EncodedNormalizedContentData>
                    {
                        new()
                        {
                            EncodedNormalizedContent = StringHelper.Base64Encode(
                                _serviceSettings.UseStructuredOutput
                                    ? JsonConvert.DeserializeObject<StructuredOutput>(normalizedRequestItem.OutlineContentData).Summary
                                    : normalizedRequestItem.OutlineContentData)
                        }
                    }
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Normalized request video creation trigger failed, normalized request status invalid",
                reference: new
                {
                    normalizedRequestItem.TenantId,
                    normalizedRequestItem.ClientId,
                    ClientDomain = normalizedRequestItem.DomainName,
                    ContentKey = normalizedRequestItem.DomainPath,
                    RefContentId = normalizedRequestItem.AppContentId,
                    NormalizedRequestId = normalizedRequestItem.Id
                },
                facility: NormalizedRequestOperationFacilities.VIDEO_CREATION_TRIGGER_FAIL,
                correlationId: normalizedRequestItem.CorrelationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid normalizedRequestId, string failedReason, string correlationId = null)
    {
        if (normalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _normalizedRequestRepository.SetStatusToFailedAsync(id: normalizedRequestId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Normalized request status fail: {failedReason ?? string.Empty}",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                ContentKey = placed.DomainPath,
                RefContentId = placed.AppContentId,
                NormalizedRequestId = placed.Id
            },
            facility: NormalizedRequestOperationFacilities.NORMALIZED_REQUEST_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}