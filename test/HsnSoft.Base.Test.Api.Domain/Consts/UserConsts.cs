using HsnSoft.Base.Test.Api.Domain.Entities;

namespace HsnSoft.Base.Test.Api.Domain.Consts;

public static class UserConsts
{
    private const string DefaultSorting = $"{nameof(User.CreationTime)} desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "Users";
    public const int EmailMaxLength = 50;
}