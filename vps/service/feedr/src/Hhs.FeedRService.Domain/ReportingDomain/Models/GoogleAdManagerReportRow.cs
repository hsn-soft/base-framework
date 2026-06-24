using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ReportingDomain.Models;

/// <summary>
/// Structured representation of a single Google Ad Manager report row
/// parsed from the NDJSON response. The columns mirror the dimensions
/// and primary metric values declared in <c>ReportService.CreateReportRequest</c>.
/// </summary>
public sealed class GoogleAdManagerReportRow
{
    // Dimensions (in request order)
    [NotNull]
    public string AdUnitIdTopLevel { get; set; } = string.Empty;
    [NotNull]
    public string AdUnitId { get; set; } = string.Empty;
    [NotNull]
    public string DemandChannel { get; set; } = string.Empty;
    [NotNull]
    public string DemandSubchannelName { get; set; } = string.Empty;
    [NotNull]
    public string OrderId { get; set; } = string.Empty;
    [NotNull]
    public string OrderName { get; set; } = string.Empty;
    [NotNull]
    public string Date { get; set; } = string.Empty;
    // Primary metrics (in request order)
    public long CodeServedCount { get; set; }
    public long Impressions { get; set; }
    public double AverageEcpm { get; set; }
    public double Revenue { get; set; }
    public long ActiveViewEligibleImpressions { get; set; }

}
