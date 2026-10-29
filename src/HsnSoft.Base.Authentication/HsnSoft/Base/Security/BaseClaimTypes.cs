namespace HsnSoft.Base.Security;

/// <summary>The claim names the framework reads and writes. They are the short JWT names, not the long .NET claim URIs.</summary>
public static class BaseClaimTypes
{
    /// <summary>Who the token is about (JWT "sub"): the user name.</summary>
    public const string UserId = "sub";

    /// <summary>The id of the user (a guid); what the audit fields keep.</summary>
    public const string Id = "user_id";

    /// <summary>The roles of the user; one claim per role.</summary>
    public const string Role = "role";
}
