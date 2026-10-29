namespace HsnSoft.Base.Authentication;

/// <summary>Authorization policy names registered by <see cref="JwtServiceCollectionExtensions.AddBaseJwtAuthentication"/>.</summary>
public static class BasePolicies
{
    /// <summary>The caller presented a valid token.</summary>
    public const string Authenticated = "authenticated";
}
