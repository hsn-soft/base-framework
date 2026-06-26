using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Tracing;

namespace Hhs.ContentService.Application.Services;

public sealed class AnalysisContentAppService(
    IServiceProvider provider,
    ITraceAccesor traceAccessor,
    ICustomerContentRepository customerContentRepository,
    IAnalysisContentRepository analysisContentRepository) : ApplicationServiceBase(provider),IAnalysisContentAppService
{
    public async Task<CreateContentResponse> CreateAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken)
    {
        var analysisId = Guid.NewGuid();

        // Fetch all customer contents by IDs
        var contentsForAnalysis = new List<CustomerContent>();
        foreach (var id in request.CustomerContentIds)
        {
            var content = await customerContentRepository.GetByIdWithTrackingAsync(id, cancellationToken);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {id}");
            contentsForAnalysis.Add(content);
        }

        var analysis = new AnalysisContent(
            analysisId,
            request.ScopeKey,
            request.DomainName,
            request.Title,
            traceAccessor?.GetCorrelationId());

        int sort = 1;

        foreach (var customerContentId in request.CustomerContentIds)
        {
            var content = contentsForAnalysis.FirstOrDefault(x => x.Id == customerContentId);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem(id: Guid.NewGuid()
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
            eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = analysis.DomainName, Items = items }
        );

        return new CreateContentResponse(Id: analysisId.ToString());
    }

    public async Task<CreateContentResponse> CreateAnalysisContentFromScopeAsync(CreateAnalysisContentFromScopeRequest request, CancellationToken cancellationToken)
    {
        // Fetch latest N successful CustomerContents with this ScopeKey and COMPLETED NormalizeStatus
        var options = new ListQueryOptions<CustomerContent> { Filter = x => x.ScopeKey == request.ScopeKey && x.NormalizeStatus == StatusNames.Completed, OrderByEntity = o => o.OrderByDescending(s => s.CreationTime), MaxResultCount = request.MaxCustomerContents };

        var successfulContents = await customerContentRepository.GetListAsync(options, cancellationToken);

        if (successfulContents.Count == 0)
            throw new InvalidOperationException($"No successful CustomerContent found for ScopeKey: {request.ScopeKey}");

        // Extract IDs for analysis
        var customerContentIds = successfulContents.Select(x => x.Id).ToList();

        // Create analysis using extracted IDs
        var analysisId = Guid.NewGuid();

        var analysis = new AnalysisContent(
            analysisId,
            request.ScopeKey,
            request.DomainName,
            request.Title,
            traceAccessor?.GetCorrelationId());

        int sort = 1;

        foreach (var customerContentId in customerContentIds)
        {
            var content = successfulContents.FirstOrDefault(x => x.Id == customerContentId);
            if (content == null)
                throw new InvalidOperationException($"CustomerContent not found: {customerContentId}");

            analysis.Items.Add(new AnalysisContentItem(id: Guid.NewGuid()
                , analysisContentId: analysisId, customerContentId: customerContentId, sortOrder: sort++));
        }

        await analysisContentRepository.InsertAsync(analysis, cancellationToken);

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
            eventMessage: new AnalysisContentCreatedEto { AnalysisContentId = analysisId, ScopeKey = analysis.ScopeKey, DomainName = analysis.DomainName, Items = items }
        );

        return new CreateContentResponse(Id: analysisId.ToString());
    }
}