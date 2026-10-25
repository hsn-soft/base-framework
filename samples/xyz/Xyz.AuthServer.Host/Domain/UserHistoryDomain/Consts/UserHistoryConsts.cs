namespace Xyz.AuthServer.Domain.UserHistoryDomain.Consts;

public static class UserHistoryConsts
{
    public const string TableName = "UserHistory";

    public const int ActionMaxLength = 32;
    public const int UserNameMaxLength = 64;
    public const int NoteMaxLength = 500;

    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    /// <summary>The auth server issued a token to the user.</summary>
    public const string Login = "login";

    /// <summary>The auth server issued a new pair of tokens for a refresh token.</summary>
    public const string Refresh = "refresh";
}
