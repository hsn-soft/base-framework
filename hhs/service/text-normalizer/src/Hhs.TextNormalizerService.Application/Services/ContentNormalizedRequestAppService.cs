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
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts.Facilities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
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

public sealed class ContentNormalizedRequestAppService(
    IServiceProvider provider,
    IOptions<TextNormalizerSettings> serviceSettings,
    IContentNormalizedRequestRepository contentNormalizedRequestRepository,
    IOutlineProvider outlineProvider,
    IScrapingProvider scrapingProvider,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IPermissionChecker permissionChecker,
    IPermissionConstraintChecker permissionConstraintChecker
) : ApplicationServiceBase(provider), IContentNormalizedRequestAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly TextNormalizerSettings _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
    private readonly IPermissionChecker _permissionChecker = permissionChecker;
    private readonly IPermissionConstraintChecker _permissionConstraintChecker = permissionConstraintChecker;

    public async Task<ContentNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await contentNormalizedRequestRepository.GetByIdOrDefaultAsync<ContentNormalizedRequestDto>(id,
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return item ?? throw new BaseHttpException((int)HttpStatusCode.NotFound);
    }

    public async Task<PagedDataResultDto<ContentNormalizedRequestDto>> GetPagedListAsync(GetContentNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default)
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

        string scopeKey = null;
        if (pagedInput.CustomerId.HasValue && pagedInput.ProductType.HasValue)
        {
            scopeKey = ScopeKeyHelper.Generate(pagedInput.CustomerId.Value, pagedInput.ProductType.Value);
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

        var filter = new FilterBuilder<ContentNormalizedRequest>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.DomainPath.Contains(pagedInput.SearchText) : null)
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null).And(pagedInput.CustomerContentId.HasValue ? e => e.CustomerContentId == pagedInput.CustomerContentId.Value : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainName) ? e => e.DomainName == pagedInput.DomainName : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainPath) ? e => e.DomainPath.Contains(pagedInput.DomainPath) : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.ReleaseTimeStart.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime >= pagedInput.ReleaseTimeStart.Value : null)
            .And(pagedInput.ReleaseTimeEnd.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime < pagedInput.ReleaseTimeEnd.Value : null)
            .Build();

        var result = await contentNormalizedRequestRepository.GetPageListAsync<ContentNormalizedRequestDto>(
            options: new PagedQueryOptions<ContentNormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? ContentNormalizedRequestConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<ContentNormalizedRequestDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<ContentNormalizedRequestDto>> GetFilterListAsync(GetContentNormalizedRequestsFilter filterInput, CancellationToken cancellationToken = default)
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

        var filter = new FilterBuilder<ContentNormalizedRequest>()
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null)
            .And(filterInput.CustomerContentId.HasValue ? e => e.CustomerContentId == filterInput.CustomerContentId.Value : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainName) ? e => e.DomainName == filterInput.DomainName : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainPath) ? e => e.DomainPath.Contains(filterInput.DomainPath) : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.ReleaseTimeStart.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime >= filterInput.ReleaseTimeStart.Value : null)
            .And(filterInput.ReleaseTimeEnd.HasValue ? e => e.ScrapingContentData != null && e.ScrapingContentData.ReleaseTime != null && e.ScrapingContentData.ReleaseTime < filterInput.ReleaseTimeEnd.Value : null)
            .Build();

        return await contentNormalizedRequestRepository.GetListAsync<ContentNormalizedRequestDto>(
            options: new ListQueryOptions<ContentNormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? ContentNormalizedRequestConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<ContentNormalizedRequestSearchDto>> GetSearchListAsync(GetContentNormalizedRequestsSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringHelper.Minimize(searchInput.SearchText);

        var filter = new FilterBuilder<ContentNormalizedRequest>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.DomainPath.Contains(searchInput.SearchText) : null)
            .Build();

        return await contentNormalizedRequestRepository.GetListAsync<ContentNormalizedRequestSearchDto>(
            options: new ListQueryOptions<ContentNormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? ContentNormalizedRequestConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(CustomerContentNormalizedStartedEto input, string correlationId = null)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ScopeKey) || input.CustomerContentId == Guid.Empty
            || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.DomainPath))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var contentNormalizedRequest = await contentNormalizedRequestRepository.GetSingleOrDefaultAsync(x
            => x.ScopeKey == input.ScopeKey && x.CustomerContentId == input.CustomerContentId);

        if (contentNormalizedRequest != null) return;

        var placedContentNormalizedRequest = await contentNormalizedRequestRepository.CreateAsync(
            scopeKey: input.ScopeKey,
            domainName: StringHelper.Minimize(input.DomainName),
            customerContentId: input.CustomerContentId,
            domainPath: StringHelper.Minimize(input.DomainPath),
            operationStatus: ContentNormalizedRequestStates.CreatedWaitForScraping,
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Content Normalized Request created",
            reference: new
            {
                ScopeKey = placedContentNormalizedRequest.ScopeKey,
                ClientDomain = placedContentNormalizedRequest.DomainName,
                ContentKey = placedContentNormalizedRequest.DomainPath,
                RefContentId = placedContentNormalizedRequest.CustomerContentId,
                RefNormalizedRequestId = placedContentNormalizedRequest.Id
            },
            facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_CREATED,
            correlationId: correlationId,
            exception: null
        ));

        // Integration Event for ContentService(set reference)
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new ContentNormalizedRequestCreatedEto(
                CustomerContentId: placedContentNormalizedRequest.CustomerContentId,
                ContentNormalizedRequestId: placedContentNormalizedRequest.Id
            ));

        // Integration Event for TextNormalizerService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new ContentNormalizedRequestScrapingStartedEto(
                ContentNormalizedRequestId: placedContentNormalizedRequest.Id
            ));
    }

    public async Task ScrapingAsync(Guid contentNormalizedRequestId)
    {
        if (contentNormalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var contentNormalizedRequest = await contentNormalizedRequestRepository.GetByIdOrDefaultAsync(contentNormalizedRequestId);
        if (contentNormalizedRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool scrapingOperationSuccess;
        string errorMessage = null;
        ScrapingContentDataModel scrapingContentData = null;
        try
        {
            ScrapingResponseDto response;
            var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == contentNormalizedRequest.ScopeKey);
            if (customerVpSetting is null) throw new ArgumentNullException(nameof(contentNormalizedRequest.ScopeKey));
            if (!_serviceSettings.SkipScrapingOperation && customerVpSetting.IsScrapingOperationActive)
            {
                response = await scrapingProvider.ScrapingAsync(new ScrapingRequestDto { DomainKey = contentNormalizedRequest.DomainName, Path = contentNormalizedRequest.DomainPath });
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
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_SCRAPING_FAIL,
                correlationId: contentNormalizedRequest.CorrelationId,
                exception: ex
            ));
            scrapingOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedContentNormalizedRequest = await contentNormalizedRequestRepository.SetScrapingResultAsync(
            id: contentNormalizedRequestId,
            isScrapingSuccess: scrapingOperationSuccess,
            errorMessage: errorMessage,
            scrapingContentData: scrapingOperationSuccess ? scrapingContentData : null);

        if (updatedContentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.ContentScrappedWaitForOutline)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Content Normalized Request scraping success",
                reference: new
                {
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_SCRAPING_SUCCESS,
                correlationId: updatedContentNormalizedRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for TextNormalizerService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new ContentNormalizedRequestOutlineStartedEto(
                    ContentNormalizedRequestId: updatedContentNormalizedRequest.Id
                ));
        }
        else
        {
            //  Integration Event for ContentService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new CustomerContentNormalizedResultEto(
                    CustomerContentId: contentNormalizedRequest.CustomerContentId,
                    IsNormalizedSuccess: contentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.OperationSuccess,
                    ContentNormalizedRequestId: contentNormalizedRequest.Id,
                    ReleaseTime: contentNormalizedRequest.ScrapingContentData?.ReleaseTime
                ));
        }
    }

    public async Task OutlineAsync(Guid contentNormalizedRequestId)
    {
        if (contentNormalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var contentNormalizedRequest = await contentNormalizedRequestRepository.GetByIdOrDefaultAsync(contentNormalizedRequestId);
        if (contentNormalizedRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool outlineOperationSuccess;
        string errorMessage = null;
        string outlineResultData = null;
        try
        {
            if (contentNormalizedRequest.ScrapingContentData != null)
            {
                if (contentNormalizedRequest.ScrapingContentData.ReleaseTime != null && contentNormalizedRequest.ScrapingContentData.ReleaseTime < DateTime.UtcNow.AddDays(-3))
                {
                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: "Content Normalized request outline skipped",
                        reference: new
                        {
                            contentNormalizedRequest.ScopeKey,
                            ClientDomain = contentNormalizedRequest.DomainName,
                            ContentKey = contentNormalizedRequest.DomainPath,
                            RefContentId = contentNormalizedRequest.CustomerContentId,
                            RefNormalizedRequestId = contentNormalizedRequest.Id
                        },
                        facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_OUTLINE_SKIPPED,
                        correlationId: contentNormalizedRequest.CorrelationId,
                        exception: null
                    ));

                    //  Integration Event for ContentService
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new CustomerContentNormalizedResultEto(
                            CustomerContentId: contentNormalizedRequest.CustomerContentId,
                            IsNormalizedSuccess: contentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.OperationSuccess,
                            ContentNormalizedRequestId: contentNormalizedRequest.Id,
                            ReleaseTime: contentNormalizedRequest.ScrapingContentData?.ReleaseTime
                        ));

                    return;
                }

                var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == contentNormalizedRequest.ScopeKey);
                if (customerVpSetting is null) throw new ArgumentNullException(nameof(contentNormalizedRequest.ScopeKey));

                string outlineInput = string.Format("{0} {1} {2}",
                    contentNormalizedRequest.ScrapingContentData.Title,
                    contentNormalizedRequest.ScrapingContentData.Spot ?? string.Empty,
                    contentNormalizedRequest.ScrapingContentData.Details ?? string.Empty
                );

                OutlineResponseDto response;
                if (!_serviceSettings.SkipOutlineOperation && customerVpSetting.IsOutlineOperationActive)
                {
                    var outlineRequest = new OutlineRequestDto { OutlinePrompt = customerVpSetting.ContentOutlinePrompt, RefContentType = ReferenceContentTypes.CUSTOMER_CONTENT, RefContentId = contentNormalizedRequest.CustomerContentId, OutlineInput = outlineInput };
                    response = await outlineProvider.OutlineAsync(outlineRequest, null, _serviceSettings.UseStructuredOutput);
                }
                else
                {
                    response = new OutlineResponseDto { RefContentId = contentNormalizedRequest.CustomerContentId, OutlinedData = outlineInput };
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
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_OUTLINE_FAIL,
                correlationId: contentNormalizedRequest.CorrelationId,
                exception: ex
            ));
            outlineOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedContentNormalizedRequest = await contentNormalizedRequestRepository.SetOutlineResultAsync(
            id: contentNormalizedRequestId,
            isNormalizedSuccess: outlineOperationSuccess,
            errorMessage: errorMessage,
            outlineResult: outlineOperationSuccess ? outlineResultData : null);

        if (updatedContentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Content Normalized Request outline success",
                reference: new
                {
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_OUTLINE_SUCCESS,
                correlationId: updatedContentNormalizedRequest.CorrelationId,
                exception: null
            ));
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new CustomerContentNormalizedResultEto(
                CustomerContentId: updatedContentNormalizedRequest.CustomerContentId,
                IsNormalizedSuccess: updatedContentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.OperationSuccess,
                ContentNormalizedRequestId: updatedContentNormalizedRequest.Id,
                ReleaseTime: updatedContentNormalizedRequest.ScrapingContentData?.ReleaseTime
            ));
    }

    public async Task VideoGenerationApprovedAsync(string scopeKey, Guid customerContentId)
    {
        var contentNormalizedRequest = await contentNormalizedRequestRepository.GetSingleOrDefaultAsync(x
            => x.ScopeKey == scopeKey && x.CustomerContentId == customerContentId);

        if (contentNormalizedRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        if (contentNormalizedRequest.OperationStatus == ContentNormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Content Normalized Request video creation trigger success",
                reference: new
                {
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CUSTOMER_CONTENT_VIDEO_TRIGGER_SUCCESS,
                correlationId: contentNormalizedRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationStartedEto(
                    ScopeKey: contentNormalizedRequest.ScopeKey,
                    DomainName: contentNormalizedRequest.DomainName,
                    ReferenceContentType: ReferenceContentTypes.CUSTOMER_CONTENT,
                    ReferenceContentId: contentNormalizedRequest.CustomerContentId,
                    EncodedNormalizedContentDatas: new List<EncodedNormalizedContentData>
                    {
                        new()
                        {
                            EncodedNormalizedContent = StringHelper.Base64Encode(
                                _serviceSettings.UseStructuredOutput
                                    ? JsonConvert.DeserializeObject<StructuredOutput>(contentNormalizedRequest.OutlineContentData).Summary
                                    : contentNormalizedRequest.OutlineContentData)
                        }
                    }
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Content Normalized Request video creation trigger failed, Content Normalized Request status invalid",
                reference: new
                {
                    contentNormalizedRequest.ScopeKey,
                    ClientDomain = contentNormalizedRequest.DomainName,
                    ContentKey = contentNormalizedRequest.DomainPath,
                    RefContentId = contentNormalizedRequest.CustomerContentId,
                    RefNormalizedRequestId = contentNormalizedRequest.Id
                },
                facility: ContentNormalizedRequestOperationFacilities.CUSTOMER_CONTENT_VIDEO_TRIGGER_FAIL,
                correlationId: contentNormalizedRequest.CorrelationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid contentNormalizedRequestId, string failedReason, string correlationId = null)
    {
        if (contentNormalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var updatedContentNormalizedRequest = await contentNormalizedRequestRepository.SetStatusToFailedAsync(id: contentNormalizedRequestId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Content Normalized Request status fail: {failedReason ?? string.Empty}",
            reference: new
            {
                updatedContentNormalizedRequest.ScopeKey,
                ClientDomain = updatedContentNormalizedRequest.DomainName,
                ContentKey = updatedContentNormalizedRequest.DomainPath,
                RefContentId = updatedContentNormalizedRequest.CustomerContentId,
                RefNormalizedRequestId = updatedContentNormalizedRequest.Id
            },
            facility: ContentNormalizedRequestOperationFacilities.CONTENT_NORMALIZED_REQUEST_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}