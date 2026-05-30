using Hhs.ContentService.Domain.ClientDomain.Entities;

namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(Client.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "Clients";
    public const int DomainNameMaxLength = 100;
}