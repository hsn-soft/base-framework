using System.Net;
using Hhs.Shared.Contracts.Events.Content;
using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAIResponses;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using Hhs.TextNormalizerService.Domain.CustomerDomain.Repositories;
using Hhs.TextNormalizerService.Domain.Enums;
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

public sealed class NormalizedAnalysisAppService : ApplicationServiceBase, INormalizedAnalysisAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly INormalizedAnalysisRepository _normalizedAnalysisRepository;
    private readonly INormalizedRequestRepository _normalizedRequestRepository;
    private readonly IOutlineProvider _outlineProvider;
    private readonly ICustomerConfigurationRepository _customerSettingsRepository;
    private readonly TextNormalizerSettings _serviceSettings;

    public NormalizedAnalysisAppService(IServiceProvider provider,
        IOptions<TextNormalizerSettings> serviceSettings,
        INormalizedAnalysisRepository normalizedAnalysisRepository,
        INormalizedRequestRepository normalizedRequestRepository,
        ICustomerConfigurationRepository customerConfigurationRepository,
        IOutlineProvider outlineProvider) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _serviceSettings = serviceSettings?.Value ?? throw new ArgumentNullException(nameof(serviceSettings));
        _customerSettingsRepository = customerConfigurationRepository;
        _normalizedAnalysisRepository = normalizedAnalysisRepository;
        _normalizedRequestRepository = normalizedRequestRepository;
        _outlineProvider = outlineProvider;
    }

    public async Task<NormalizedAnalysisDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _normalizedAnalysisRepository.GetSingleOrDefaultAsync<NormalizedAnalysisDto>(
            predicate: x => x.Id == id && x.IsDeleted == false,
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<NormalizedAnalysisDto>> GetPagedListAsync(GetNormalizedAnalysisPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
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

        var filter = new FilterBuilder<NormalizedAnalysis>()
            .And(e => e.IsDeleted == false)
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.DomainName.Contains(pagedInput.SearchText) : null)
            .And(pagedInput.ClientId.HasValue ? e => e.ClientId == pagedInput.ClientId.Value : null)
            .And(pagedInput.AnalysisContentId.HasValue ? e => e.AnalysisContentId == pagedInput.AnalysisContentId.Value : null)
            .And(pagedInput.CreationTimeStart.HasValue ? e => e.CreationTime >= pagedInput.CreationTimeStart.Value : null)
            .And(pagedInput.CreationTimeEnd.HasValue ? e => e.CreationTime < pagedInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainName) ? e => e.DomainName == pagedInput.DomainName : null)
            .And(pagedInput.OperationStatus.HasValue ? e => e.OperationStatus == pagedInput.OperationStatus.Value : null)
            .And(pagedInput.AnalysisDateStart.HasValue ? e => e.AnalysisDate >= pagedInput.AnalysisDateStart.Value : null)
            .And(pagedInput.AnalysisDateEnd.HasValue ? e => e.AnalysisDate < pagedInput.AnalysisDateEnd.Value : null)
            .Build();

        var result = await _normalizedAnalysisRepository.GetPageListAsync<NormalizedAnalysisDto>(
            options: new PagedQueryOptions<NormalizedAnalysis>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? NormalizedAnalysisConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<NormalizedAnalysisDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<NormalizedAnalysisDto>> GetFilterListAsync(GetNormalizedAnalysisFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
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

        var filter = new FilterBuilder<NormalizedAnalysis>()
            .And(e => e.IsDeleted == false)
            .And(filterInput.ClientId.HasValue ? e => e.ClientId == filterInput.ClientId.Value : null)
            .And(filterInput.AnalysisContentId.HasValue ? e => e.AnalysisContentId == filterInput.AnalysisContentId.Value : null)
            .And(filterInput.CreationTimeStart.HasValue ? e => e.CreationTime >= filterInput.CreationTimeStart.Value : null)
            .And(filterInput.CreationTimeEnd.HasValue ? e => e.CreationTime < filterInput.CreationTimeEnd.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainName) ? e => e.DomainName == filterInput.DomainName : null)
            .And(filterInput.OperationStatus.HasValue ? e => e.OperationStatus == filterInput.OperationStatus.Value : null)
            .And(filterInput.AnalysisDateStart.HasValue ? e => e.AnalysisDate >= filterInput.AnalysisDateStart.Value : null)
            .And(filterInput.AnalysisDateEnd.HasValue ? e => e.AnalysisDate < filterInput.AnalysisDateEnd.Value : null)
            .Build();

        return await _normalizedAnalysisRepository.GetListAsync<NormalizedAnalysisDto>(
            options: new ListQueryOptions<NormalizedAnalysis>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? NormalizedAnalysisConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(AnalysisContentNormalizedStartedEto input, string correlationId = null)
    {
        if (input == null || input.TenantId == Guid.Empty || input.ClientId == Guid.Empty || input.AnalysisDate == default || input.AppContentIdList is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var analysisEntity = await _normalizedAnalysisRepository.FindByUniqueKeysAsync(input.ClientId, input.AnalysisContentId);
        if (analysisEntity != null) return;

        var normalizedRequestList = await _normalizedRequestRepository.GetListAsync(new ListQueryOptions<NormalizedRequest> { Filter = x => x.ClientId == input.ClientId && input.AppContentIdList.Contains(x.AppContentId) });

        if (normalizedRequestList is { Count: < 1 })
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        normalizedRequestList = normalizedRequestList
            .OrderBy(x => input.AppContentIdList.IndexOf(x.AppContentId))
            .ToList();

        var analysisReferenceList = normalizedRequestList.Select(x => new AnalysisReferenceModel
        {
            AppContentId = x.AppContentId,
            NormalizedRequestId = x.Id,
            AnalysisDataModel = StructuredOutputWithScrapingDateMapper(x.ScrapingContentData,x.OutlineContentData)
        }).ToList();

        var placed = await _normalizedAnalysisRepository.CreateAsync(
            tenantId: input.TenantId,
            clientId: input.ClientId,
            domainName: input.DomainName,
            analysisContentId: input.AnalysisContentId,
            analysisDate: input.AnalysisDate,
            analysisReferenceList: analysisReferenceList,
            operationStatus: NormalizedAnalysisStates.CreatedWaitForOutline,
            correlationId: correlationId);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"Normalized analysis created",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                RefContentId = placed.AnalysisContentId,
                NormalizedRequestId = placed.Id
            },
            facility: NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_CREATED,
            correlationId: correlationId,
            exception: null
        ));

        // Integration Event for ContentService(set reference)
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisContentNormalizedAnalysisCreatedEto(
                AnalysisContentId: placed.AnalysisContentId,
                NormalizedAnalysisId: placed.Id
            ));

        // Integration Event for TextNormalizerService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new NormalizedAnalysisOutlineStartedEto(
                NormalizedAnalysisId: placed.Id
            ));
    }

    private AnalysisDataModel StructuredOutputWithScrapingDateMapper(ScrapingContentDataModel scrapingDataModel,
        string structuredOutputString)
    {
        try
        {
            var structuredOutput = JsonConvert.DeserializeObject<StructuredOutput>(structuredOutputString);
            return new AnalysisDataModel()
            {
                ImageUrl = scrapingDataModel.ImageUrl,
                Title = structuredOutput.Title,
                Details = structuredOutput.Summary,
                Spot = structuredOutput.Spot
            };
        }
        catch
        {
            return Mapper.Map<ScrapingContentDataModel, AnalysisDataModel>(scrapingDataModel);
        }

    }

    public async Task OutlineAsync(Guid normalizedAnalysisId)
    {
        if (normalizedAnalysisId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var normalizedAnalysisItem = await _normalizedAnalysisRepository.GetByIdOrDefaultAsync(normalizedAnalysisId);
        if (normalizedAnalysisItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool outlineOperationSuccess;
        string errorMessage = null;
        List<KeyValuePair<Guid, string>> outlineResultData = null;
        try
        {
            if (normalizedAnalysisItem.AnalysisReferenceList is { Count: > 0 })
            {
                var clientSettings = await _customerSettingsRepository.FindByUniqueKeysAsync(normalizedAnalysisItem.ClientId);
                if (clientSettings is null) throw new ArgumentNullException(nameof(normalizedAnalysisItem.ClientId));

                var outlineInput = normalizedAnalysisItem.AnalysisReferenceList.Select(x => new OutlineRequestDto
                {
                    OutlinePrompt = clientSettings.NormalizerSetting.AnalysisOutlineContentPrompt,
                    RefContentType = ReferenceContentTypes.ANALYSIS_CONTENT,
                    RefContentId = x.AppContentId,
                    OutlineInput = (_serviceSettings.UseStructuredOutput || clientSettings.NormalizerSetting.IsForceContentDetailInAnalyseActive) ? x.AnalysisDataModel.Details : x.AnalysisDataModel.Title.Replace(":", "") + " : " + (x.AnalysisDataModel.Spot ?? x.AnalysisDataModel.Details)
                }).ToList();

                List<OutlineResponseDto> response;
                OutlineResponseDto introResponse, outroResponse;
                if (!_serviceSettings.SkipOutlineOperation && clientSettings.NormalizerSetting.IsOutlineOperationActive)
                {
                    response = await _outlineProvider.OutlineAsync(outlineInput,_serviceSettings.UseStructuredOutput);
                    if (outlineInput.Count != response.Count)
                    {
                        throw new Exception("OUTLINE RESPONSE HAS ERROR");
                    }
                    introResponse = await _outlineProvider.OutlineAsync(new OutlineRequestDto
                    {
                        OutlinePrompt = clientSettings.NormalizerSetting.AnalysisOutlineIntroPrompt,
                        RefContentType = ReferenceContentTypes.APP_REQUEST_CONTENT,
                        RefContentId = Guid.CreateVersion7(), // Fake content id
                        OutlineInput = "."
                    }, "gpt-4.1-mini");
                    outroResponse = await _outlineProvider.OutlineAsync(new OutlineRequestDto
                    {
                        OutlinePrompt = clientSettings.NormalizerSetting.AnalysisOutlineOutroPrompt,
                        RefContentType = ReferenceContentTypes.APP_REQUEST_CONTENT,
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
                        response[0].OutlinedData = introResponse.OutlinedData.Replace("\"","") + "." + response[0].OutlinedData;
                    }

                    //Manipulate last content
                    if (!string.IsNullOrWhiteSpace(outroResponse?.OutlinedData))
                    {
                        response[^1].OutlinedData = response[^1].OutlinedData + "." + (outroResponse.OutlinedData.Replace("\"",""));
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
                reference: new
                {
                    normalizedAnalysisItem.TenantId,
                    normalizedAnalysisItem.ClientId,
                    ClientDomain = normalizedAnalysisItem.DomainName,
                    RefContentId = normalizedAnalysisItem.AnalysisContentId,
                    NormalizedAnalysisId = normalizedAnalysisItem.Id
                },
                facility: NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_OUTLINE_FAIL,
                correlationId: normalizedAnalysisItem.CorrelationId,
                exception: ex
            ));
            outlineOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updated = await _normalizedAnalysisRepository.SetOutlineResultAsync(
            id: normalizedAnalysisId,
            isOutlineSuccess: outlineOperationSuccess,
            errorMessage: errorMessage,
            outlineResultList: outlineOperationSuccess ? outlineResultData : null);

        if (updated.OperationStatus == NormalizedAnalysisStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Normalized analysis outline success",
                reference: new
                {
                    normalizedAnalysisItem.TenantId,
                    normalizedAnalysisItem.ClientId,
                    ClientDomain = normalizedAnalysisItem.DomainName,
                    RefContentId = normalizedAnalysisItem.AnalysisContentId,
                    NormalizedAnalysisId = normalizedAnalysisItem.Id
                },
                facility: NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_OUTLINE_SUCCESS,
                correlationId: updated.CorrelationId,
                exception: null
            ));
        }

        //  Integration Event for ContentService
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AnalysisContentNormalizedResultEto(
                AnalysisContentId: updated.AnalysisContentId,
                IsNormalizedSuccess: updated.OperationStatus == NormalizedAnalysisStates.OperationSuccess,
                NormalizedAnalysisId: updated.Id
            ));
    }

    public async Task VideoGenerationApprovedAsync(VideoGenerationApprovedEto input)
    {
        var normalizedAnalysisItem = await _normalizedAnalysisRepository.FindByUniqueKeysAsync(input.ClientId, input.ReferenceContentId);
        if (normalizedAnalysisItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        if (normalizedAnalysisItem.OperationStatus == NormalizedAnalysisStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Normalized Analysis video creation trigger success",
                reference: new
                {
                    normalizedAnalysisItem.TenantId,
                    normalizedAnalysisItem.ClientId,
                    ClientDomain = normalizedAnalysisItem.DomainName,
                    RefContentId = normalizedAnalysisItem.AnalysisContentId,
                    NormalizedAnalysisId = normalizedAnalysisItem.Id
                },
                facility: NormalizedAnalysisOperationFacilities.VIDEO_CREATION_TRIGGER_SUCCESS,
                correlationId: normalizedAnalysisItem.CorrelationId,
                exception: null
            ));

            // Integration Event for VideoGeneratorService
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationStartedEto(
                    TenantId: normalizedAnalysisItem.TenantId,
                    ClientId: normalizedAnalysisItem.ClientId,
                    DomainName: normalizedAnalysisItem.DomainName,
                    ReferenceContentType: ReferenceContentTypes.ANALYSIS_CONTENT,
                    ReferenceContentId: normalizedAnalysisItem.AnalysisContentId,
                    EncodedNormalizedContentDatas: Mapper
                        .Map<List<AnalysisReferenceModel>, List<EncodedNormalizedContentData>>(normalizedAnalysisItem
                            .AnalysisReferenceList)
                ));
        }
        else
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Normalized Analysis video creation trigger failed, normalized Analysis status invalid",
                reference: new
                {
                    normalizedAnalysisItem.TenantId,
                    normalizedAnalysisItem.ClientId,
                    ClientDomain = normalizedAnalysisItem.DomainName,
                    RefContentId = normalizedAnalysisItem.AnalysisContentId,
                    NormalizedAnalysisId = normalizedAnalysisItem.Id
                },
                facility: NormalizedAnalysisOperationFacilities.VIDEO_CREATION_TRIGGER_FAIL,
                correlationId: normalizedAnalysisItem.CorrelationId,
                exception: null
            ));
        }
    }

    public async Task SetStatusToFailedAsync(Guid normalizedAnalysisId, string failedReason, string correlationId = null)
    {
        if (normalizedAnalysisId == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _normalizedAnalysisRepository.SetStatusToFailedAsync(id: normalizedAnalysisId, failedReason: failedReason);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: $"Normalized analysis status fail: {failedReason ?? string.Empty}",
            reference: new
            {
                placed.TenantId,
                placed.ClientId,
                ClientDomain = placed.DomainName,
                RefContentId = placed.AnalysisContentId,
                NormalizedAnalysisId = placed.Id
            },
            facility: NormalizedAnalysisOperationFacilities.NORMALIZED_ANALYSIS_STATUS_FAIL,
            correlationId: correlationId,
            exception: null
        ));
    }
}