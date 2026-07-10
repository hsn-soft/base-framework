using System.Net;
using Hhs.ContentService.Domain.Constants;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class AnalysisContentAppService(
    IServiceProvider provider,
    ITraceAccesor traceAccessor,
    IAnalysisContentRepository analysisContentRepository,
    IContentVideoGenerationLimitRepository contentVideoGenerationLimitRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    ICustomerContentRepository customerContentRepository,
    ICustomerContentVisitRepository customerContentVisitRepository)
    : ApplicationServiceBase(provider), IAnalysisContentAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task<CreateContentResponse> CreateTestAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken)
    {
        // Fetch all customer contents by IDs
        var contentsForAnalysis = new List<CustomerContent>();
        foreach (var id in request.CustomerContentIds)
        {
            var content = await customerContentRepository.GetByIdWithTrackingAsync(id, cancellationToken);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {id}");
            contentsForAnalysis.Add(content);
        }

        var analysisId = Guid.CreateVersion7();
        var analysisDate = DateTime.UtcNow.Date;

        var analysis = new AnalysisContent(id: analysisId,
            scopeKey: request.ScopeKey,
            analysisDate: analysisDate,
            traceAccessor?.GetCorrelationId());
        int sort = 1;

        foreach (var customerContentId in request.CustomerContentIds)
        {
            var content = contentsForAnalysis.FirstOrDefault(x => x.Id == customerContentId);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem(id: Guid.CreateVersion7()
                , analysisContentId: analysisId, customerContentId: customerContentId, sortOrder: sort++));
        }

        await analysisContentRepository.InsertAsync(analysis, cancellationToken);

        var items = request.CustomerContentIds
            .Select((id, index) =>
            {
                var content = contentsForAnalysis.First(x => x.Id == id);

                return new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 };
            })
            .ToList();

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: analysis.CorrelationId,
            eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = request.DomainName, Items = items }
        );

        return new CreateContentResponse(Id: analysisId.ToString());
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
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AnalysisContentVideoGenerationSkippedEarlyTime,
                reference: new { customerVpSetting.ScopeKey, Type = "DomainName", Key = customerVpSetting.DomainName },
                facility: Milestones.AnalysisContentVideoGenerationSkippedEarlyTime,
                correlationId: correlationId,
                exception: null
            ));

            _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
            return;
        }

        var analysisDate = DateTime.UtcNow.Date;

        // check client Analysis video generation quote available
        long clientDailyAnalysisVideoGenerationHistoryCount = await contentVideoGenerationLimitRepository.GetCustomerVideoHistoryCountAsync(customerVpSetting.ScopeKey,
            analysisDate, VideoGenerationTypes.AnalysisVideoGeneration);

        var clientQuoteResult = customerVpSetting.DailyAnalysisVideoGenerationLimit - clientDailyAnalysisVideoGenerationHistoryCount <= 0
            ? new KeyValuePair<bool, string>(false, Milestones.AnalysisContentVideoGenerationSkippedDailyLimit)
            : new KeyValuePair<bool, string>(true, Milestones.AnalysisContentVideoGenerationApproved);
        if (clientQuoteResult.Key)
        {
            List<CustomerContent> selectedContents;

            // Primary: today's published & normalized content ordered by visit count
            var dailyContentIds = await customerContentRepository.GetCustomerDailyAnalysisContentIdsAsync(customerVpSetting.ScopeKey);
            if (dailyContentIds is { Count: > 0 })
            {
                long? minVisit = customerVpSetting.DailyAnalysisContentMinVisitCount > 0
                    ? (long?)customerVpSetting.DailyAnalysisContentMinVisitCount
                    : null;

                long? maxItem = customerVpSetting.DailyAnalysisVideoItemLimit > 0
                    ? (long?)customerVpSetting.DailyAnalysisVideoItemLimit
                    : null;

                var visitList = await customerContentVisitRepository.GetContentIdsVisitCountsAsync(
                    customerContentIds: dailyContentIds,
                    isMaxCountOrdered: true,
                    customerContentOrderedLimit: maxItem,
                    customerContentVisitedCountLimit: minVisit);

                if (visitList is { Count: > 0 })
                {
                    var visitedIds = visitList.Select(x => x.CustomerContentId).ToList();
                    var fetchOptions = new ListQueryOptions<CustomerContent> { Filter = x => visitedIds.Contains(x.Id) };
                    var fetched = await customerContentRepository.GetListAsync(fetchOptions);
                    selectedContents = visitedIds
                        .Select(id => fetched.FirstOrDefault(c => c.Id == id))
                        .Where(c => c != null)
                        .ToList();
                }
                else
                {
                    selectedContents = [];

                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: "ANALYSIS_CONTENT_VIDEO_GENERATION_SKIPPED_NO_VISITED_CONTENT",
                        reference: new { customerVpSetting.ScopeKey, Type = "DomainName", Key = customerVpSetting.DomainName },
                        facility: "ANALYSIS_CONTENT_VIDEO_GENERATION_SKIPPED_NO_VISITED_CONTENT",
                        correlationId: correlationId,
                        exception: null
                    ));
                }
            }
            else
            {
                selectedContents = [];

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: "ANALYSIS_CONTENT_VIDEO_GENERATION_SKIPPED_NO_COMPLETED_CONTENT",
                    reference: new { customerVpSetting.ScopeKey, Type = "DomainName", Key = customerVpSetting.DomainName },
                    facility: "ANALYSIS_CONTENT_VIDEO_GENERATION_SKIPPED_NO_COMPLETED_CONTENT",
                    correlationId: correlationId,
                    exception: null
                ));
            }

            // Fallback: most recently completed content regardless of release date
            // if (selectedContents.Count == 0)
            // {
            //     _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}",
            //         customerVpSetting.DomainName, "FALLBACK", "ANALYSIS_USING_RECENT_COMPLETED_CONTENT");
            //
            //     var fallbackOptions = new ListQueryOptions<CustomerContent>
            //     {
            //         Filter = x => x.ScopeKey == customerVpSetting.ScopeKey
            //                       && x.NormalizeStatus == NormalizeStatusNames.Completed,
            //         OrderByEntity = o => o.OrderByDescending(s => s.CreationTime),
            //         MaxResultCount = analysisItemLimit
            //     };
            //     selectedContents = await customerContentRepository.GetListAsync(fallbackOptions);
            // }

            if (selectedContents.Count < customerVpSetting.DailyAnalysisVideoItemLimit)
            {
                _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
                return;
            }

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: clientQuoteResult.Value,
                reference: new { customerVpSetting.ScopeKey, Type = "DomainName", Key = customerVpSetting.DomainName },
                facility: clientQuoteResult.Value,
                correlationId: correlationId,
                exception: null
            ));

            var analysisContentId = Guid.CreateVersion7();
            var analysis = new AnalysisContent(
                id: analysisContentId,
                scopeKey: customerVpSetting.ScopeKey,
                analysisDate: analysisDate,
                correlationId);
            int sort = 1;
            foreach (var content in selectedContents)
            {
                analysis.Items.Add(new AnalysisContentItem(
                    id: Guid.CreateVersion7(),
                    analysisContentId: analysisContentId,
                    customerContentId: content.Id,
                    sortOrder: sort++));
            }

            await analysisContentRepository.InsertAsync(analysis);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Analysis Content created",
                reference: new { customerVpSetting.ScopeKey, Type = nameof(AnalysisContent), Key = analysisContentId },
                facility: Facilities.AnalysisContentCreated,
                correlationId: correlationId,
                exception: null
            ));

            var items = selectedContents
                .Select((content, index) => new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 })
                .ToList();

            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                correlationId: analysis.CorrelationId,
                eventMessage: new AnalysisContentCreatedEto { ScopeKey = analysis.ScopeKey, DomainName = customerVpSetting.DomainName, AnalysisContentId = analysisContentId, Items = items }
            );
        }
        else
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: clientQuoteResult.Value,
                reference: new { customerVpSetting.ScopeKey, Type = "DomainName", Key = customerVpSetting.DomainName },
                facility: clientQuoteResult.Value,
                correlationId: correlationId,
                exception: null
            ));
        }

        _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", customerVpSetting.DomainName, "END");
    }

    public async Task<ForceAnalysisVideoGenerationResultDto> ForceAnalysisVideoGenerationQueryAsync(ForceAnalysisVideoGenerationRequestDto input)
    {
        try
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

            // Fetch latest N successful CustomerContents with this ScopeKey and COMPLETED NormalizeStatus
            var options = new ListQueryOptions<CustomerContent>
            {
                Filter = x => x.ScopeKey == input.ScopeKey
                              && x.NormalizeStatus == NormalizeStatusNames.Completed
                              && input.CustomerContentIds.Contains(x.Id),
                OrderByEntity = o => o.OrderByDescending(s => s.CreationTime)
            };

            var successfulContents = await customerContentRepository.GetListAsync(options);

            if (successfulContents.Count == 0)
                throw new InvalidOperationException($"No successful CustomerContent found for ScopeKey: {input.ScopeKey}");

            // Extract IDs for analysis
            var customerContentIds = successfulContents.Select(x => x.Id).ToList();

            // Create analysis using extracted IDs
            var analysisId = Guid.CreateVersion7();
            var analysisDate = DateTime.UtcNow.Date;

            var analysis = new AnalysisContent(id: analysisId,
                scopeKey: input.ScopeKey,
                analysisDate: analysisDate,
                traceAccessor?.GetCorrelationId());
            int sort = 1;

            foreach (var customerContentId in customerContentIds)
            {
                var content = successfulContents.FirstOrDefault(x => x.Id == customerContentId);
                if (content == null)
                    throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

                analysis.Items.Add(new AnalysisContentItem(id: Guid.CreateVersion7()
                    , analysisContentId: analysisId, customerContentId: customerContentId, sortOrder: sort++));
            }

            await analysisContentRepository.InsertAsync(analysis);

            var items = customerContentIds
                .Select((id, index) =>
                {
                    var content = successfulContents.First(x => x.Id == id);

                    return new AnalysisNormalizeItem { CustomerContentId = content.Id, ContentKey = content.ContentKey, SortOrder = index + 1 };
                })
                .ToList();

            await EventBus.PublishAsync(
                parentMessage: ParentIntegrationEvent,
                correlationId: analysis.CorrelationId,
                eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = customerVpSetting.DomainName, Items = items }
            );

            return new ForceAnalysisVideoGenerationResultDto { operationSuccess = true };
        }
        catch (Exception e)
        {
            return new ForceAnalysisVideoGenerationResultDto { errorMessage = e.Message };
        }
    }
}