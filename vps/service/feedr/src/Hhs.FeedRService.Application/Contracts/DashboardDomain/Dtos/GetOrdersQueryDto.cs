using JetBrains.Annotations;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

/// <summary>
/// Input for querying distinct orders visible in stored report data.
/// At least one of NetworkCode, AdUnitTopLevelCode, AdUnitCode, or ClientId must be supplied.
/// </summary>
public sealed class GetOrdersQueryDto
{
    /// <summary>Google Ad Manager network code (optional).</summary>
    [CanBeNull]
    public string NetworkCode { get; set; }

    /// <summary>Top-level AdUnit code (optional).</summary>
    [CanBeNull]
    public string AdUnitTopLevelCode { get; set; }

    /// <summary>Client-specific AdUnit code (optional).</summary>
    [CanBeNull]
    public string AdUnitCode { get; set; }

    /// <summary>Client identifier (optional).</summary>
    public Guid? ClientId { get; set; }
}
