namespace Hhs.AuthServer.Models;

internal static class Constants
{
    public static class TokenResponse
    {
        public const string BearerTokenType = "Bearer";
    }

    public static class GrantTypes
    {
        public const string Password = "password";
        public const string AuthorizationCode = "authorization_code";
        public const string ClientCredentials = "client_credentials";
        public const string RefreshToken = "refresh_token";
        public const string Implicit = "implicit";
        public const string Saml2Bearer = "urn:ietf:params:oauth:grant-type:saml2-bearer";
        public const string JwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        public const string DeviceCode = "urn:ietf:params:oauth:grant-type:device_code";
        public const string TokenExchange = "urn:ietf:params:oauth:grant-type:token-exchange";
    }

    public static class StandardScopes
    {
        /// <summary>REQUIRED. Informs the Authorization Server that the Client is making an OpenID Connect request. If the <c>openid</c> scope value is not present, the behavior is entirely unspecified.</summary>
        public const string OpenId = "openid";

        /// <summary>OPTIONAL. This scope value requests access to the End-User's default profile Claims, which are: <c>name</c>, <c>family_name</c>, <c>given_name</c>, <c>middle_name</c>, <c>nickname</c>, <c>preferred_username</c>, <c>profile</c>, <c>picture</c>, <c>website</c>, <c>gender</c>, <c>birthdate</c>, <c>zoneinfo</c>, <c>locale</c>, and <c>updated_at</c>.</summary>
        public const string Profile = "profile";

        /// <summary>OPTIONAL. This scope value requests access to the <c>email</c> and <c>email_verified</c> Claims.</summary>
        public const string Email = "email";

        /// <summary>OPTIONAL. This scope value requests access to the <c>address</c> Claim.</summary>
        public const string Address = "address";

        /// <summary>OPTIONAL. This scope value requests access to the <c>phone_number</c> and <c>phone_number_verified</c> Claims.</summary>
        public const string Phone = "phone";

        /// <summary>This scope value MUST NOT be used with the OpenID Connect Implicit Client Implementer's Guide 1.0. See the OpenID Connect Basic Client Implementer's Guide 1.0 (http://openid.net/specs/openid-connect-implicit-1_0.html#OpenID.Basic) for its usage in that subset of OpenID Connect.</summary>
        public const string OfflineAccess = "offline_access";
    }
}