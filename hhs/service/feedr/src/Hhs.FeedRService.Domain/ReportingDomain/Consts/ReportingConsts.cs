namespace Hhs.FeedRService.Domain.ReportingDomain.Consts;

public static class ReportingConsts
{
    public const string RawResponseTableName = "RawGoogleAdManagerResponses";
    public const string AdNetworkTableName = "AdNetworks";
    public const string AdUnitTopLevelTableName = "AdUnitTopLevels";
    public const string AdUnitClientTableName = "AdUnitClients";
    public const string DailyReportResponseTableName = "DailyReportResponses";
    public const string MongoToPostgresMappingTableName = "MongoToPostgresMappings";

    public const int NetworkCodeMaxLength = 64;
    public const int AdUnitCodeMaxLength = 64;
    public const int ClientNameMaxLength = 256;
    public const int JobNameMaxLength = 256;
    public const int StatusMaxLength = 32;

    public const string DefaultDailyReportSorting = "ReportDate desc";
}
