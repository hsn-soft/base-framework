using Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public sealed record CreateContentResponse(string Id);

public sealed record CreateAnalysisContentRequest(
    string ScopeKey,
    string DomainName,
    string Title,
    List<Guid> CustomerContentIds);

public sealed record CreateAnalysisContentFromScopeRequest(
    string ScopeKey,
    string DomainName,
    string Title,
    int MaxCustomerContents = 5);

public interface IAnalysisContentAppService : IEventApplicationService
{
    Task AnalysisVideoGenerationQueryAsync(AnalysisVideoGenerationQueryEto input, [CanBeNull] string correlationId = null);

    Task<CreateContentResponse> CreateTestAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken);

    Task<ForceAnalysisVideoGenerationResultDto> ForceAnalysisVideoGenerationQueryAsync(ForceAnalysisVideoGenerationRequestDto input, string correlationId = null);
}