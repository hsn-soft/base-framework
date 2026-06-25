using Hhs.TextNormalizerService.Domain.InfraDomain.Entities;

namespace Hhs.TextNormalizerService.Domain.InfraDomain.Consts;

public static class EventInboxMessageConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(EventInboxMessage.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "EventInboxMessages";
    public const int CorrelationIdMaxLength = 50;
    public const int EventNameMaxLength = 256;
    public const int StatusMaxLength = 50;
    public const int ErrorMessageMaxLength = 1000;
    public const int PayloadMaxLength = 5000;
}
