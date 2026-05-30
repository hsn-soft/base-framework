using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class MonthlyResponsesTotalsDto
{
    [NotNull]
    public MonthlyResponsesTotalsItemDto[] Series { get; set; } = [];

    public int Total => Series.Sum(x => x.Count);
}

// ((data.Count * 100)/ (monthlyResponsesTotalsDto?.Total ?? 1))

public sealed class MonthlyResponsesTotalsItemDto
{
    [NotNull]
    public string Name { get; set; }

    [CanBeNull]
    public string Border { get; set; }

    [NotNull]
    public int Count { get; set; } = 0;
}