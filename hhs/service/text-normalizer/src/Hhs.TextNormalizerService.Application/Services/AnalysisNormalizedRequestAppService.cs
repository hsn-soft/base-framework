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
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts.Facilities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Settings;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class AnalysisNormalizedRequestAppService(
    IServiceProvider provider,
    IOptions<TextNormalizerSettings> serviceSettings,
    IAnalysisNormalizedRequestRepository analysisNormalizedRequestRepository,
    IContentNormalizedRequestRepository contentNormalizedRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IOutlineProvider outlineProvider
) : ApplicationServiceBase(provider), IAnalysisNormalizedRequestAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly TextNormalizerSettings _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));

    public async Task<AnalysisNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await analysisNormalizedRequestRepository.GetByIdOrDefaultAsync<AnalysisNormalizedRequestDto>(id,
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return item ?? throw new BaseHttpException((int)HttpStatusCode.NotFound);
    }

    public async Task<PagedDataResultDto<AnalysisNormalizedRequestDto>> GetPagedListAsync(GetAnalysisNormalizedRequestPaged pagedInput, CancellationToken cancellationToken = default)
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

        pagedInput.SearchText = StringHelper.Minimize(pagedInput.SearchText);
        pagedInput.DomainName = StringHelper.Minimize(pagedInput.DomainName);

        if (pagedInput.CreationTimeEnd.HasValue)
        {
            pagedInput.CreationTimeEnd = pagedInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (pagedInput.AnalysisDateEnd.HasValue)
        {
            pagedInput.AnalysisDateEnd = pagedInput.AnalysisDateEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<AnalysisNormalizedRequest>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.DomainName.Contains(pagedInput.SearchText) : null)
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null).And(pagedInput.AnalysisContentId.HasValue ? e => e.AnalysisContentId == pagedInput.AnalysisContentId.Value : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainName) ? e => e.DomainName == pagedInput.DomainName : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.AnalysisDateStart.HasValue ? e => e.AnalysisDate >= pagedInput.AnalysisDateStart.Value : null)
            .And(pagedInput.AnalysisDateEnd.HasValue ? e => e.AnalysisDate < pagedInput.AnalysisDateEnd.Value : null)
            .Build();

        var result = await analysisNormalizedRequestRepository.GetPageListAsync<AnalysisNormalizedRequestDto>(
            options: new PagedQueryOptions<AnalysisNormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? AnalysisNormalizedRequestConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<AnalysisNormalizedRequestDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<AnalysisNormalizedRequestDto>> GetFilterListAsync(GetAnalysisNormalizedRequestFilter filterInput, CancellationToken cancellationToken = default)
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

        if (filterInput.CreationTimeEnd.HasValue)
        {
            filterInput.CreationTimeEnd = filterInput.CreationTimeEnd.Value.AddDays(1);
        }

        if (filterInput.AnalysisDateEnd.HasValue)
        {
            filterInput.AnalysisDateEnd = filterInput.AnalysisDateEnd.Value.AddDays(1);
        }

        var filter = new FilterBuilder<AnalysisNormalizedRequest>()
            .And(!string.IsNullOrWhiteSpace(scopeKey) ? e => e.ScopeKey == scopeKey : null)
            .And(filterInput.AnalysisContentId.HasValue ? e => e.AnalysisContentId == filterInput.AnalysisContentId.Value : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainName) ? e => e.DomainName == filterInput.DomainName : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.AnalysisDateStart.HasValue ? e => e.AnalysisDate >= filterInput.AnalysisDateStart.Value : null)
            .And(filterInput.AnalysisDateEnd.HasValue ? e => e.AnalysisDate < filterInput.AnalysisDateEnd.Value : null)
            .Build();

        return await analysisNormalizedRequestRepository.GetListAsync<AnalysisNormalizedRequestDto>(
            options: new ListQueryOptions<AnalysisNormalizedRequest>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AnalysisNormalizedRequestConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(AnalysisContentNormalizedStartedEto input, string correlationId = null)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ScopeKey) || input.AnalysisContentId == Guid.Empty
            || input.AnalysisDate == default || input.CustomerContentIdList is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var analysisNormalizedRequest = await analysisNormalizedRequestRepository.GetSingleOrDefaultAsync(x
            => x.ScopeKey == input.ScopeKey && x.AnalysisContentId == input.AnalysisContentId);

        if (analysisNormalizedRequest != null) return;

        var contentNormalizedRequests = await contentNormalizedRequestRepository.GetListAsync(
            new ListQueryOptions<ContentNormalizedRequest>
            {
                Filter = x => x.ScopeKey == input.ScopeKey
                              && input.CustomerContentIdList.Contains(x.CustomerContentId)
            });

        if (contentNormalizedRequests is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        contentNormalizedRequests = contentNormalizedRequests
            .OrderBy(x => input.CustomerContentIdList.IndexOf(x.CustomerContentId))
            .ToList();

        var analysisReferenceList = contentNormalizedRequests.Select(x =>
            new AnalysisReferenceModel { CustomerContentId = x.CustomerContentId, ContentNormalizedRequestId = x.Id, AnalysisDataModel = StructuredOutputWithScrapingDateMapper(x.ScrapingContentData, x.OutlineContentData) }).ToList();

        var placedAnalysisNormalizedRequest = await analysisNormalizedRequestRepository.CreateAsync(
            scopeKey: input.ScopeKey,
            domainName: input.DomainName,
            analysisContentId: input.AnalysisContentId,
            analysisDate: input.AnalysisDate,
            analysisReferenceList: analysisReferenceList,
            operationStatus: AnalysisNormalizedRequestStates.CreatedWaitForOutline,
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Analysis Normalized Request created",
            reference: new { placedAnalysisNormalizedRequest.ScopeKey, ClientDomain = placedAnalysisNormalizedRequest.DomainName, RefContentId = placedAnalysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = placedAnalysisNormalizedRequest.Id },
            facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_CREATED,
            correlationId: correlationId,
            exception: null
        ));

        // Integration Event for ContentService(set reference)
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisNormalizedRequestCreatedEto(
                AnalysisContentId: placedAnalysisNormalizedRequest.AnalysisContentId,
                AnalysisNormalizedRequestId: placedAnalysisNormalizedRequest.Id
            ));

        // Integration Event for TextNormalizerService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisNormalizedRequestOutlineStartedEto(
                AnalysisNormalizedRequestId: placedAnalysisNormalizedRequest.Id
            ));
    }

    private AnalysisDataModel StructuredOutputWithScrapingDateMapper(ScrapingContentDataModel scrapingDataModel,
        string structuredOutputString)
    {
        try
        {
            var structuredOutput = JsonConvert.DeserializeObject<StructuredOutput>(structuredOutputString);
            return new AnalysisDataModel() { ImageUrl = scrapingDataModel.ImageUrl, Title = structuredOutput.Title, Details = structuredOutput.Summary, Spot = structuredOutput.Spot };
        }
        catch
        {
            return Mapper.Map<ScrapingContentDataModel, AnalysisDataModel>(scrapingDataModel);
        }
    }

    public async Task OutlineAsync(Guid analysisNormalizedRequestId)
    {
        if (analysisNormalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var analysisNormalizedRequest = await analysisNormalizedRequestRepository.GetByIdOrDefaultAsync(analysisNormalizedRequestId);
        if (analysisNormalizedRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool outlineOperationSuccess;
        string errorMessage = null;
        List<KeyValuePair<Guid, string>> outlineResultData = null;
        try
        {
            if (analysisNormalizedRequest.AnalysisReferenceList is { Count: > 0 })
            {
                var customerVpSetting = await customerVpSettingRepository.GetSingleOrDefaultAsync(x => x.ScopeKey == analysisNormalizedRequest.ScopeKey);
                if (customerVpSetting is null) throw new ArgumentNullException(nameof(analysisNormalizedRequest.ScopeKey));

                var outlineInput = analysisNormalizedRequest.AnalysisReferenceList.Select(x => new OutlineRequestDto
                {
                    OutlinePrompt = customerVpSetting.AnalysisOutlineContentPrompt,
                    RefContentType = ReferenceContentTypes.ANALYSIS_CONTENT,
                    RefContentId = x.CustomerContentId,
                    OutlineInput = (_serviceSettings.UseStructuredOutput || customerVpSetting.IsForceContentDetailInAnalyseActive) ? x.AnalysisDataModel.Details : x.AnalysisDataModel.Title.Replace(":", "") + " : " + (x.AnalysisDataModel.Spot ?? x.AnalysisDataModel.Details)
                }).ToList();

                List<OutlineResponseDto> response;
                OutlineResponseDto introResponse, outroResponse;
                if (!_serviceSettings.SkipOutlineOperation && customerVpSetting.IsOutlineOperationActive)
                {
                    response = await outlineProvider.OutlineAsync(outlineInput, _serviceSettings.UseStructuredOutput);
                    if (outlineInput.Count != response.Count)
                    {
                        throw new Exception("OUTLINE RESPONSE HAS ERROR");
                    }

                    introResponse = await outlineProvider.OutlineAsync(new OutlineRequestDto
                    {
                        OutlinePrompt = customerVpSetting.AnalysisOutlineIntroPrompt,
                        RefContentType = ReferenceContentTypes.CUSTOMER_CONTENT,
                        RefContentId = Guid.CreateVersion7(), // Fake content id
                        OutlineInput = "."
                    }, "gpt-4.1-mini");
                    outroResponse = await outlineProvider.OutlineAsync(new OutlineRequestDto
                    {
                        OutlinePrompt = customerVpSetting.AnalysisOutlineOutroPrompt,
                        RefContentType = ReferenceContentTypes.CUSTOMER_CONTENT,
                        RefContentId = Guid.CreateVersion7(), // Fake content id
                        OutlineInput = "."
                    }, "gpt-4.1-mini");
                }
                else
                {
                    response = outlineInput.Select(x => new OutlineResponseDto { RefContentId = x.RefContentId, OutlinedData = x.OutlineInput }).ToList();
                    introResponse = new OutlineResponseDto
                    {
                        RefContentId = Guid.CreateVersion7(), // Fake content id
                        OutlinedData = "İyi günler, günün öne çıkan haberleriyle karşınızdayız."
                    };
                    outroResponse = new OutlineResponseDto
                    {
                        RefContentId = Guid.CreateVersion7(), // Fake content id
                        OutlinedData = "Günün öne çıkan gelişmelerini aktardık. Tekrar görüşmek üzere"
                    };
                }

                if (response is { Count: > 0 })
                {
                    //Manipulate first content
                    if (!string.IsNullOrWhiteSpace(introResponse?.OutlinedData))
                    {
                        response[0].OutlinedData = introResponse.OutlinedData.Replace("\"", "") + "." + response[0].OutlinedData;
                    }

                    //Manipulate last content
                    if (!string.IsNullOrWhiteSpace(outroResponse?.OutlinedData))
                    {
                        response[^1].OutlinedData = response[^1].OutlinedData + "." + (outroResponse.OutlinedData.Replace("\"", ""));
                    }

                    outlineResultData = response.Select(x => new KeyValuePair<Guid, string>(x.RefContentId, x.OutlinedData)).ToList();
                    if (outlineResultData is { Count: > 0 } && !outlineResultData.Any(x => string.IsNullOrWhiteSpace(x.Value)))
                    {
                        outlineOperationSuccess = true;
                    }
                    else throw new Exception("OUTLINE CONTENT DATA NOT FOUND");
                }
                else throw new Exception("OUTLINE RESPONSE IS NULL");
            }
            else throw new Exception("OUTLINE SCRAPING DATA NOT FOUND");
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: ex.Message,
                reference: new { analysisNormalizedRequest.ScopeKey, ClientDomain = analysisNormalizedRequest.DomainName, RefContentId = analysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = analysisNormalizedRequest.Id },
                facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_OUTLINE_FAIL,
                correlationId: analysisNormalizedRequest.CorrelationId,
                exception: ex
            ));
            outlineOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updatedAnalysisNormalizedRequest = await analysisNormalizedRequestRepository.SetOutlineResultAsync(
            id: analysisNormalizedRequestId,
            isOutlineSuccess: outlineOperationSuccess,
            errorMessage: errorMessage,
            outlineResultList: outlineOperationSuccess ? outlineResultData : null);

        if (updatedAnalysisNormalizedRequest.OperationStatus == AnalysisNormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Analysis Normalized Request outline success",
                reference: new { analysisNormalizedRequest.ScopeKey, ClientDomain = analysisNormalizedRequest.DomainName, RefContentId = analysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = analysisNormalizedRequest.Id },
                facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_OUTLINE_SUCCESS,
                correlationId: updatedAnalysisNormalizedRequest.CorrelationId,
                exception: null
            ));
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisContentNormalizedResultEto(
                AnalysisContentId: updatedAnalysisNormalizedRequest.AnalysisContentId,
                IsNormalizedSuccess: updatedAnalysisNormalizedRequest.OperationStatus == AnalysisNormalizedRequestStates.OperationSuccess,
                AnalysisNormalizedRequestId: updatedAnalysisNormalizedRequest.Id
            ));
    }

    public async Task VideoGenerationApprovedAsync(string scopeKey, Guid analysisContentId)
    {
        var analysisNormalizedRequest = await analysisNormalizedRequestRepository.GetSingleOrDefaultAsync(x
            => x.ScopeKey == scopeKey && x.AnalysisContentId == analysisContentId);

        if (analysisNormalizedRequest == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        if (analysisNormalizedRequest.OperationStatus == AnalysisNormalizedRequestStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Analysis Normalized Request video creation trigger success",
                reference: new { analysisNormalizedRequest.ScopeKey, ClientDomain = analysisNormalizedRequest.DomainName, RefContentId = analysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = analysisNormalizedRequest.Id },
                facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_CONTENT_VIDEO_TRIGGER_SUCCESS,
                correlationId: analysisNormalizedRequest.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationStartedEto(
                    ScopeKey: analysisNormalizedRequest.ScopeKey,
                    DomainName: analysisNormalizedRequest.DomainName,
                    ReferenceContentType: ReferenceContentTypes.ANALYSIS_CONTENT,
                    ReferenceContentId: analysisNormalizedRequest.AnalysisContentId,
                    EncodedNormalizedContentDatas: Mapper
                        .Map<List<AnalysisReferenceModel>, List<EncodedNormalizedContentData>>(analysisNormalizedRequest
                            .AnalysisReferenceList)
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Analysis Normalized Request video creation trigger failed, Analysis Normalized Request status invalid",
                reference: new { analysisNormalizedRequest.ScopeKey, ClientDomain = analysisNormalizedRequest.DomainName, RefContentId = analysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = analysisNormalizedRequest.Id },
                facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_CONTENT_VIDEO_TRIGGER_FAIL,
                correlationId: analysisNormalizedRequest.CorrelationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid analysisNormalizedRequestId, string failedReason, string correlationId = null)
    {
        if (analysisNormalizedRequestId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var updatedAnalysisNormalizedRequest = await analysisNormalizedRequestRepository.SetStatusToFailedAsync(id: analysisNormalizedRequestId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Analysis Normalized Request status fail: {failedReason ?? string.Empty}",
            reference: new { updatedAnalysisNormalizedRequest.ScopeKey, ClientDomain = updatedAnalysisNormalizedRequest.DomainName, RefContentId = updatedAnalysisNormalizedRequest.AnalysisContentId, RefNormalizedRequestId = updatedAnalysisNormalizedRequest.Id },
            facility: AnalysisNormalizedRequestOperationFacilities.ANALYSIS_NORMALIZED_REQUEST_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}