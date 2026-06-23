using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class WeeklyResponsesAnalysisDto
{
    [NotNull]
    public string[] Categories { get; set; } = [];

    [NotNull]
    public WeeklyResponsesAnalysisItemDto[] Series { get; set; } = [];
}

public sealed class WeeklyResponsesAnalysisItemDto
{
    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public int[] Data { get; set; } = [];
}