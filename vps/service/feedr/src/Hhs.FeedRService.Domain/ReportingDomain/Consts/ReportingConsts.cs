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

    // Dashboard report field lengths
    public const int NetworkMaxLength = 256;
    public const int AdUnitNameMaxLength = 256;
    public const int DemandChannelMaxLength = 256;
    public const int DemandSubchannelNameMaxLength = 256;
    public const int OrderIdMaxLength = 256;
    public const int OrderNameMaxLength = 512;

    public const string DefaultDailyReportSorting = "ReportDate desc";
}
