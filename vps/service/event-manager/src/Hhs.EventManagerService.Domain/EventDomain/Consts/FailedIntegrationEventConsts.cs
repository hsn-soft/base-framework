namespace Hhs.EventManagerService.Domain.EventDomain.Consts;

public static class FailedIntegrationEventConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "FailedIntegrationEvents";
    public const int CorrelationIdMaxLength = 50;
    public const int ProducerMaxLength = 100;
    public const int ChannelMaxLength = 100;
    public const int UserIdMaxLength = 100;
    public const int UserRoleUniqueNameMaxLength = 100;
    public const int FailedMessageTypeNameMaxLength = 100;
}