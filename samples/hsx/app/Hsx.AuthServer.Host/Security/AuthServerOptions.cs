namespace Hsx.AuthServer.Security;

/// <summary>The section <c>Auth</c> of the settings: how the refresh token of a user is kept by its client.</summary>
public sealed class AuthServerOptions
{
    public const string SectionName = "Auth";

    /// <summary>How many days a refresh token lives; a token that is used is replaced by a new one that lives as long.</summary>
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>The most scope keys a token carries; 0 is no limit. The keys of all the roles of a user are in it and the headers of a request are 32 KB, so some six hundred keys is where a request is refused: a limit or a selection of keys is decided when a user comes near it.</summary>
    public int MaxScopeKeysInToken { get; set; }

    /// <summary>The name of the cookie that holds the refresh token (HttpOnly, SameSite=Strict, Secure on https).</summary>
    public string RefreshCookieName { get; set; } = "refreshToken";

    /// <summary>The path the cookie is sent to: the public path of the auth server, as the gateway shows it.</summary>
    public string RefreshCookiePath { get; set; } = "/";
}
