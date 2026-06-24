namespace Hhs.EventManagerService.Domain.InfraDomain.Consts;

public static class EventInboxMessageConsts
{
    public const string CollectionName = "EventInboxMessages";
    public const int CorrelationIdMaxLength = 50;
    public const int EventNameMaxLength = 256;
    public const int StatusMaxLength = 50;
    public const int ErrorMessageMaxLength = 1000;
}
