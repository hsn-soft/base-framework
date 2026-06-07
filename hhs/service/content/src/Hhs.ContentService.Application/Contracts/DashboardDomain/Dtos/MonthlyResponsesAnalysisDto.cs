using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class MonthlyResponsesAnalysisDto
{
    [NotNull]
    public string[] Categories { get; set; } = [];

    [NotNull]
    public MonthlyResponsesAnalysisItemDto[] Series { get; set; } = [];
}

public sealed class MonthlyResponsesAnalysisItemDto
{
    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public int[] Data { get; set; } = [];
}