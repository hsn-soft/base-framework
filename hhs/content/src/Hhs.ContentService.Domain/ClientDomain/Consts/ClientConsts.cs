namespace Hhs.ContentService.Domain.ClientDomain.Consts;

public static class ClientConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "Clients";
    public const int DomainNameMaxLength = 100;
    public const int SubdomainNameMaxLength = 150;
}