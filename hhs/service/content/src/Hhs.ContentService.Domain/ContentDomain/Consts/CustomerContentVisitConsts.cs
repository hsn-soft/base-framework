using Hhs.ContentService.Domain.ContentDomain.Entities;

namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class CustomerContentVisitConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(CustomerContentVisit.VisitTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "CustomerContentVisits";
    public const int NameMaxLength = 256;
    public const int ScopeKeyMaxLength = 128;
    public const int VisitResponseMaxLength = 30;
}