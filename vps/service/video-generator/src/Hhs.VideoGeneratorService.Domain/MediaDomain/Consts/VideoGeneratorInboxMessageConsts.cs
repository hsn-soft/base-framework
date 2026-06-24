namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;

public static class VideoGeneratorInboxMessageConsts
{
    public const string TableName = "video_generator_inbox_messages";
    public const int EventNameMaxLength = 256;
    public const int StatusMaxLength = 50;
    public const int ErrorMessageMaxLength = 1000;
}
