namespace Hhs.FeedRService.Application.Consts;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents — decoupled from any published integration event name.
/// </summary>
public static class Facilities
{
    public const string GenerateSummaryReportTriggered = "GENERATE_SUMMARY_REPORT";
    public const string TestQueryTriggered = "TEST_QUERY";
    public const string DerivePendingReportsTriggered = "DERIVE_PENDING_REPORTS";
    public const string ReportPersisted = "REPORT_PERSISTENCE";
    public const string ReportDerived = "REPORT_DERIVATION";
}
