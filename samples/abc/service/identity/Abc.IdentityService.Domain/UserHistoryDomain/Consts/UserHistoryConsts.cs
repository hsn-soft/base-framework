namespace Abc.IdentityService.Domain.UserHistoryDomain.Consts;

public static class UserHistoryConsts
{
    public const string TableName = "user_history";

    public const int ActionMaxLength = 32;
    public const int UserNameMaxLength = 64;
    public const int NoteMaxLength = 500;

    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    /// <summary>The auth server issued a token to the user.</summary>
    public const string Login = "login";
}
