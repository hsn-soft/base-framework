namespace Hhs.FeedRService.Application;

public static class ApplicationErrorCodes
{
    public const string UnknownOriginAddress = "Error:FeedRService:100001";
    public const string InvalidOriginAddress = "Error:FeedRService:100002";
    public const string InvalidDomainAddress = "Error:FeedRService:100003";
    public const string IncompatibleDomainAddress = "Error:FeedRService:100004";
    public const string EntityNotFound = "Error:FeedRService:100005";
}