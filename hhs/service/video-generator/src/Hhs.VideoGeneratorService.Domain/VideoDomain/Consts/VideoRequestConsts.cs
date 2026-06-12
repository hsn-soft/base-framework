namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Consts;

public static class VideoRequestConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "VideoRequests";
    public const int DomainNameMaxLength = 100;
    public const int ExternalVideoTraceIdMaxLength = 100;
    public const int StorageVideoTraceIdMaxLength = 100;
}