using HsnSoft.Base.EventBus;

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
    Task<CreateContentResponse> CreateAnalysisContentAsync(CreateAnalysisContentRequest request, CancellationToken cancellationToken);
    Task<CreateContentResponse> CreateAnalysisContentFromScopeAsync(CreateAnalysisContentFromScopeRequest request, CancellationToken cancellationToken);
}