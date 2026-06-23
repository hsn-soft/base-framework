using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class DailyResponsesTotalsDto
{
    [NotNull]
    public int[] Series { get; set; } = [];

    [NotNull]
    public string[] Labels { get; set; } = [];
}