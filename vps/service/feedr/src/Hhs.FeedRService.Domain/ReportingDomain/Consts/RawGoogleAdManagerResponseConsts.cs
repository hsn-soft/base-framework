namespace Hhs.FeedRService.Domain.ReportingDomain.Consts;

public static class RawGoogleAdManagerResponseConsts
{
    public const string CollectionName = "RawGoogleAdManagerResponses";
    public const string TableName = "RawGoogleAdManagerResponses";

    public const int ClientNameMaxLength = 256;
    public const int RequestIdMaxLength = 256;
    public const int JobNameMaxLength = 256;
    public const int StatusMaxLength = 32;
    public const int NetworkMaxLength = 256;
    public const int AdUnitIdTopLevelMaxLength = 256;
    public const int AdUnitIdMaxLength = 256;
    public const int RawResponseMaxLength = 10000;
    public const int ProcessingErrorMaxLength = 1000;
}
