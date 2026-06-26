using System.Diagnostics.CodeAnalysis;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class ForceAnalysisVideoGenerationRequestDto
{
    [NotNull] public string ScopeKey { get; set; } = string.Empty;
    public List<Guid> CustomerContentIds { get; set; } = [];
}