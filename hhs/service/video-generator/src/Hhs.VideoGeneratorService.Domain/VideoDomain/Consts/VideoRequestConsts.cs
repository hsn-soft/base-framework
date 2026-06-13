using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;

namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Consts;

public static class VideoRequestConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(VideoRequest.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "VideoRequests";
    public const int ScopeKeyMaxLength = 128;
    public const int DomainNameMaxLength = 100;
    public const int ExternalVideoTraceIdMaxLength = 100;
    public const int StorageVideoTraceIdMaxLength = 100;
}